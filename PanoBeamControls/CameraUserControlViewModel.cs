using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PanoBeamLib;

namespace PanoBeam.Controls
{
    public class CameraUserControlViewModel : ViewModelBase
    {
        private VideoDeviceCollection _videoDeviceCollection;
        private readonly VideoCapture _videoCapture;

        public Action<int, int> AddCropAdorner;

        public CameraUserControlViewModel()
        {
            _videoDeviceCollection = new VideoDeviceCollection();
            _videoCapture = VideoCapture.Instance;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (_videoDeviceCollection.Count == 1)
            {
                Camera = _videoDeviceCollection[0].MonikerString;
            }
            else
            {
                Camera = Configuration.Configuration.Instance.Settings.Camera.MonikerString;
            }
        }

        private ImageSource _imageSource;

        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public IntPtr ParentWindow { get; set; }

        public void SetClippingRectangle(Rect rectangle)
        {
            _videoCapture.ClippingRectangle = rectangle;
        }

        public string Camera
        {
            get => Configuration.Configuration.Instance.Settings.Camera.MonikerString;
            set
            {
                Disconnect();
                Configuration.Configuration.Instance.Settings.Camera.MonikerString = value;
                _videoCapture.SetCamera(Configuration.Configuration.Instance.Settings.Camera.MonikerString);
                OnPropertyChanged();
            }
        }

        public VideoDeviceCollection Cameras
        {
            get => _videoDeviceCollection;
            set
            {
                if (Equals(value, _videoDeviceCollection)) return;
                _videoDeviceCollection = value;
                OnPropertyChanged();
            }
        }

        #region Commands
        private bool _connectCanExecute = true;
        private ICommand _connectCommand;

        public ICommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new CommandHandler(Connect, param => _connectCanExecute));
            }
        }

        private bool _disconnectCanExecute;
        private ICommand _disconnectCommand;

        public ICommand DisconnectCommand
        {
            get
            {
                return _disconnectCommand ?? (_disconnectCommand = new CommandHandler(Disconnect, param => _disconnectCanExecute));
            }
        }

        private readonly bool _settingsCanExecute = true;
        private ICommand _settingsCommand;

        public ICommand SettingsCommand
        {
            get
            {
                return _settingsCommand ?? (_settingsCommand = new CommandHandler(Settings, param => _settingsCanExecute));
            }
        }

        #endregion

        private void Connect()
        {
            _connectCanExecute = false;
            _videoCapture.FirstFrame += FirstFrame;
            _videoCapture.Frame += Frame;
            //_videoCapture.Threshold = Threshold;
            _videoCapture.Start();
            _disconnectCanExecute = true;
        }

        private void Disconnect()
        {
            _disconnectCanExecute = false;
            _videoCapture.Stop();
            _connectCanExecute = true;
        }

        private void Settings()
        {
            _videoCapture.ShowCameraSettings(ParentWindow);
        }

        private void FirstFrame(BitmapSource bitmapSource, int width, int height)
        {
            ImageSource = bitmapSource;
            AddCropAdorner(width, height);
        }

        private void Frame(BitmapSource bitmapSource)
        {
            ImageSource = bitmapSource;
        }
    }
}