using System.Drawing;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PanoBeam.Events;
using PanoBeam.Events.Data;
using PanoBeam.Events.Events;
using PanoBeamLib;
using System;
using PanoBeam.Configuration;
using PanoBeam.Mapper;

namespace PanoBeam
{
    /// <summary>
    /// Interaction logic for Screen.xaml
    /// </summary>
    public partial class ScreenView
    {
        public ScreenView()
        {
            InitializeComponent();
        }

        private PanoScreen _screen;

        public Size Resolution { get; set; }

        public int Overlap { get; set; }

        private bool _isShiftPressed;

        public void Initialize(PanoScreen screen)
        {
            _screen = screen;
            var width = Resolution.Width;
            var height = Resolution.Height;
            Width = width;
            Height = height;
            Image1.Width = width;
            Image1.Height = height;
            WarpControl1.Initialize(screen);

            _screen.SetPattern(
                Configuration.Configuration.Instance.Settings.PatternSize,
                new Size(Configuration.Configuration.Instance.Settings.PatternCountX, Configuration.Configuration.Instance.Settings.PatternCountY),
                Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap, false);

            _screen.UpdateProjectorsFromConfig(ProjectorMapper.MapProjectorsData(Configuration.Configuration.Instance.Projectors));

            //EventHelper.SubscribeEvent<CalibrationDataChanged, CalibrationData>(OnCalibrationDataChanged);
            EventHelper.SubscribeEvent<ControlPointsMoved, ControlPointData>(OnControlPointsMoved);
            //EventHelper.SubscribeEvent<ApplicationReady, EventArgs>(OnApplicationReady);
        }   
        
        public void Refresh(ControlPointsMode controlPointsMode, bool wireframeVisible)
        {
            Dispatcher.Invoke(() => {
                WarpControl1.UpdateWarpControl(controlPointsMode);
                WarpControl1.SetVisibility(controlPointsMode, wireframeVisible);
            });
        }

        //private void OnApplicationReady(EventArgs obj)
        //{
        //    //_screen.RefreshPattern(false);
        //    //WarpControl1.UpdateWarpControl(ControlPointsMode.None);
        //}

        public void UpdateWarpControl()
        {
            if (_screen.SetPattern(Configuration.Configuration.Instance.Settings.PatternSize, GetPatternCount(), Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap, false))
            {
                WarpControl1.UpdateWarpControl(Configuration.Configuration.Instance.Settings.ControlPointsMode);
            }
            WarpControl1.SetVisibility(Configuration.Configuration.Instance.Settings.ControlPointsMode, Configuration.Configuration.Instance.Settings.ShowWireframe);
        }

        private Size GetPatternCount()
        {
            return new Size(Configuration.Configuration.Instance.Settings.PatternCountX, Configuration.Configuration.Instance.Settings.PatternCountY);
        }

        //private void OnCalibrationDataChanged(CalibrationData calibrationData)
        //{
        //    _immediateWarp = calibrationData.ImmediateWarp;
        //    if (_screen.SetPattern(calibrationData.PatternSize, calibrationData.PatternCount, calibrationData.ControlPointsInsideOverlap, false))
        //    {
        //        WarpControl1.UpdateWarpControl(calibrationData.ControlPointsMode);
        //    }
        //    WarpControl1.SetVisibility(calibrationData.ControlPointsMode, calibrationData.WireframeVisible);
        //}

        private void OnControlPointsMoved(ControlPointData controlPointData)
        {
            if (Configuration.Configuration.Instance.Settings.ImmediateWarp)
            {
                _screen.Warp();
            }
        }

        //public void UpdateWarpControl(bool controlPointsVisible, bool wireframeVisible, bool immediateWarp)
        //{
        //    WarpControl1.UpdateWarpControl(controlPointsVisible, wireframeVisible);
        //}

        public void ShowImage(BitmapImage image)
        {
            Image1.Source = image;
        }

        public void ShowImage(string file)
        {
            Dispatcher.Invoke(() =>
            {
                var image = new BitmapImage(new Uri(file));
                Image1.Source = image;
            });
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _isShiftPressed = false;
            }
            else if (e.Key == Key.D0)
            {
                WarpControl1.SetActiveProjector(0);
            }
            else if (e.Key == Key.D1)
            {
                WarpControl1.SetActiveProjector(1);
            }
            else if (e.Key == Key.Escape)
            {
                WarpControl1.DeactivateProjectors();
            }
            else if (e.Key == Key.W)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _screen.Warp();
                Mouse.OverrideCursor = null;
            }
            else if(e.Key == Key.B)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _screen.Blend();
                Mouse.OverrideCursor = null;
            }
            else //if (WarpControl1.HasActiveControlPoint)
            {
                WarpControl1.KeyPressed(e, _isShiftPressed);
            }
            //Koordinaten anzeigen
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                _isShiftPressed = true;
            }
        }
    }
}
