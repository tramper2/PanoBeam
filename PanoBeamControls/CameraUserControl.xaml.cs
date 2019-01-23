using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace PanoBeam.Controls
{
    /// <summary>
    /// Interaction logic for CameraUserControl.xaml
    /// </summary>
    public partial class CameraUserControl
    {
        CroppingAdorner _clp;
        FrameworkElement _felCur;
        private int _imageWidth;
        private int _imageHeight;
        private readonly CameraUserControlViewModel _viewModel;

        public CameraUserControl()
        {
            InitializeComponent();
            Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            _viewModel = new CameraUserControlViewModel {ParentWindow = Process.GetCurrentProcess().MainWindowHandle};
            _viewModel.AddCropAdorner += AddCropAdorner;
            DataContext = _viewModel;
        }

        public Rect GetClippingRectangle()
        {
            if (_clp == null) return new Rect(0,0, _imageWidth, _imageHeight);
            return _clp.GetScaledClippingRectangle(_imageWidth, _imageHeight);
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            _viewModel.DisconnectCommand.Execute(null);
        }

        private void AddCropAdorner(int width, int height)
        {
            _imageWidth = width;
            _imageHeight = height;
            var thread = new Thread(() =>
            {
                do
                {
                    Dispatcher.Invoke(() =>
                    {
                        Image.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        Image.Arrange(new Rect(0, 0, width, height));
                        Image.UpdateLayout();
                    });
                    Thread.Sleep(100);
                } while (Image.ActualWidth <= 0);
                Dispatcher.Invoke(() => { AddCropToElement(Image); });
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            thread.Start();
        }

        private void AddCropToElement(FrameworkElement fel)
        {
            if (_felCur != null)
            {
                return;
            }
            var rcInterior = new Rect(
                0,
                0,
                fel.ActualWidth,
                fel.ActualHeight);
            var adornerLayer = AdornerLayer.GetAdornerLayer(fel);
            _clp = new CroppingAdorner(fel, rcInterior);
            _felCur = fel;
            var color = Colors.Black;
            color.A = 180;
            _clp.Fill = new SolidColorBrush(color);
            adornerLayer.Add(_clp);
            adornerLayer.UpdateLayout();

            var dx = 1d / _clp.ClippingRectangle.Width * _imageWidth;
            var dy = 1d / _clp.ClippingRectangle.Height * _imageHeight;
            _clp.SetClippingRectangle(new Rect(
                Configuration.Configuration.Instance.Settings.ClippingRectangle.X / dx,
                Configuration.Configuration.Instance.Settings.ClippingRectangle.Y / dy,
                Configuration.Configuration.Instance.Settings.ClippingRectangle.Width / dx,
                Configuration.Configuration.Instance.Settings.ClippingRectangle.Height / dy
                ));

            UpdateClippingRectangle();
            _clp.CropChanged += (sender, args) =>
            {
                UpdateClippingRectangle();
            };
            Image.SizeChanged += ImageSizeChanged;
        }

        private void ImageSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            UpdateClippingRectangle();
        }

        private void UpdateClippingRectangle()
        {
            var rect = _clp.GetScaledClippingRectangle(_imageWidth, _imageHeight);
            _viewModel.SetClippingRectangle(rect);
        }
    }
}
