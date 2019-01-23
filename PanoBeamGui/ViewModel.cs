using System;
using System.Drawing;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;
using PanoBeam.Configuration;
using PanoBeam.Controls;
using PanoBeamLib;
using PanoBeam.Events;
using PanoBeam.Events.Events;
using System.Linq;
using PanoBeam.Mapper;

namespace PanoBeam
{
    public class ViewModel : ViewModelBase
    {
        private readonly ScreenView _screenView;
        private readonly MainWindow _mainWindow;
        private readonly PanoScreen _screen;

        public CameraUserControl CameraUserControl { get; }
        public CalibrationUserControl CalibrationUserControl { get; }
        public BlendingUserControl BlendingUserControl { get; }
        public TestImagesUserControl TestImagesUserControl { get; }
        private string _configFilename;
        private Point _mousePosition;

        public ViewModel(ScreenView screen, MosaicInfo mosaicInfo, MainWindow mainWindow)
        {
            //if (Helpers.IsDevComputer)
            //{
            //    _configFilename = @"C:\Temp\PanoBeam.config";
            //}
            _screenView = screen;
            _mainWindow = mainWindow;
            CameraUserControl = new CameraUserControl();
            CalibrationUserControl = new CalibrationUserControl();
            BlendingUserControl = new BlendingUserControl();
            TestImagesUserControl = new TestImagesUserControl();
            CalibrationUserControl.Start += CalibrationUserControlOnStart;
            TestImagesUserControl.ShowImage += TestImagesUserControlOnShowImage;

            _screen = new PanoScreen
            {
                Resolution = _screenView.Resolution,
                Overlap = _screenView.Overlap,
                SaveCursorPosition = () => { _mousePosition = Win32.GetMousePosition(); },
                RestoreCursorPosition = () =>
                {
                    if (_mousePosition != null) Win32.SetCursorPos(_mousePosition.X, _mousePosition.Y);
                }
            };
            _screen.AddProjectors(mosaicInfo.DisplayId0, mosaicInfo.DisplayId1);
            //_screen.LoadDefaults();
            CalibrationUserControl.Initialize();
            BlendingUserControl.Initialize(_screen.Projectors);
            _screen.CalculationProgress += ScreenOnCalculationProgress;

            //_screen.SetPattern(50, new Size(8, 9));
        }

        private void ScreenOnCalculationProgress(float progress)
        {
            _mainWindow.ReportProgress(progress);
        }

        public void Initialize()
        {
            _screenView.Initialize(_screen);
            CalibrationUserControl.Refresh();
            BlendingUserControl.Refresh();

            EventHelper.SubscribeEvent<SettingsChanged, EventArgs>(OnSettingsChanged);
        }

        private void OnSettingsChanged(EventArgs obj)
        {
            _screenView.UpdateWarpControl();
            _screenView.Refresh(Configuration.Configuration.Instance.Settings.ControlPointsMode, Configuration.Configuration.Instance.Settings.ShowWireframe);
            SaveSettings();
        }

        public void CloseScreen()
        {
            _screenView.Close();
        }

        /*private void OnCalibrationDataChanged(CalibrationData data)
        {
            _screen.SetPattern(data.PatternSize, data.PatternCount);
            _screenView.UpdateWarpControl(data.ControlPointsVisible, data.WireframeVisible, data.ImmediateWarp);
            //_screen.InitializeControlPoints();
            //var controlPoints = new[] {_screen.GetControlPoints(0), _screen.GetControlPoints(1)};
            //_screenView.UpdateControlPoints(controlPoints);
        }*/

        public bool IsScreenVisible
        {
            get => _screenView.IsVisible;
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                if (_screenView.IsVisible)
                {
                    _screenView.Hide();
                }
                else
                {
                    _screenView.Show();
                    _mainWindow.Activate();
                }
                OnPropertyChanged();
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged(nameof(ScreenButtonToolTip));
            }
        }

        public string ScreenButtonToolTip => IsScreenVisible ? "Screen-Fenster ausblenden" : "Screen-Fenster anzeigen";

        private void TestImagesUserControlOnShowImage(BitmapImage image)
        {
            _screenView.ShowImage(image);
        }

        private void CalibrationUserControlOnStart(int patternSize, Size patternCount, bool keepCorners)
        {
            CalibrationUserControl.SetInProgress(true);
            //_screen.UpdateSettings(keepCorners);
            //SaveSettings();
            _screen.CalibrationDone = () =>
            {
                CalibrationUserControl.SetInProgress(false);
                _screenView.Refresh(ControlPointsMode.None, false);
                _screen.Warp();
                _mainWindow.CalibrationDone();
            };
            _screen.CalibrationError = (message) =>
            {
                CalibrationUserControl.SetInProgress(false);
                _mainWindow.CalibrationError(message);
            };
            _screen.CalibrationCanceled = () =>
            {
                CalibrationUserControl.SetInProgress(false);
            };
            _screen.SetPattern(patternSize, patternCount, false, false);
            // TODO Marco: Kamera oder File
            if(Helpers.CameraCalibration)
            {
                _screen.AwaitProjectorsReady = _mainWindow.AwaitProjectorsReady;
            }
            else
            {
                _screen.AwaitProjectorsReady = AwaitProjectorsReadyAuto;
            }
            _screen.AwaitCalculationsReady = _mainWindow.AwaitCalculationsReady;
            var rect = CameraUserControl.GetClippingRectangle();
            _screen.ClippingRectangle = rect.GetRectangle();
            if(_screen.ControlPointsAdjusted)
            {
                _screen.ShowImage = ShowImage;
                _screen.Calibrate(false);
            }
            else
            {
                _screen.Calibrate(true);
            }
        }

        private void ShowImage(string file)
        {
            _screenView.ShowImage(file);
        }

        private void AwaitProjectorsReadyAuto(Action continueAction, Action calibrationCanceled, CalibrationSteps[] calibrationSteps)
        {
            continueAction();
        }
        
        #region Commands
        private ICommand _warpCommand;
        public ICommand WarpCommand
        {
            get
            {
                return _warpCommand ?? (_warpCommand = new CommandHandler(Warp, param => true));
            }
        }

        private ICommand _unWarpCommand;
        public ICommand UnWarpCommand
        {
            get
            {
                return _unWarpCommand ?? (_unWarpCommand = new CommandHandler(UnWarp, param => true));
            }
        }

        private ICommand _blendCommand;
        public ICommand BlendCommand
        {
            get
            {
                return _blendCommand ?? (_blendCommand = new CommandHandler(Blend, param => true));
            }
        }

        private ICommand _unBlendCommand;
        public ICommand UnBlendCommand
        {
            get
            {
                return _unBlendCommand ?? (_unBlendCommand = new CommandHandler(UnBlend, param => true));
            }
        }

        private ICommand _loadCommand;
        public ICommand LoadCommand
        {
            get
            {
                return _loadCommand ?? (_loadCommand = new CommandHandler(Load, param => true));
            }
        }

        private ICommand _saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand ?? (_saveCommand = new CommandHandler(Save, param => true));
            }
        }

        #endregion

        private void Warp()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.Warp();
            Mouse.OverrideCursor = null;
        }

        private void UnWarp()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.UnWarp();
            Mouse.OverrideCursor = null;
        }

        private void Blend()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.Blend();
            Mouse.OverrideCursor = null;
        }

        private void UnBlend()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _screen.UnBlend();
            Mouse.OverrideCursor = null;
        }

        private string GetProgramDataDirectory()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "alphasoft marco wittwer", "PanoBeam");
        }

        private string GetDefaultDataDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void Load()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "PanoBeam Config (*.config)|*.config",
                InitialDirectory = GetDefaultDataDirectory()
            };
            if (ofd.ShowDialog() == true)
            {
                _configFilename = ofd.FileName;
            }
            else
            {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            var xmlSerializer = new XmlSerializer(typeof(Configuration.Configuration));
            Configuration.Configuration config;
            using (var reader = new XmlTextReader(_configFilename))
            {
                config = (Configuration.Configuration)xmlSerializer.Deserialize(reader);
            }
            Configuration.Configuration.Instance.UpdateConfig(config);
            _screen.Update(config.Settings.PatternSize, new Size(config.Settings.PatternCountX, config.Settings.PatternCountY), config.Settings.KeepCorners, config.Settings.ControlPointsInsideOverlap);
            _screen.UpdateProjectorsFromConfig(ProjectorMapper.MapProjectorsData(Configuration.Configuration.Instance.Projectors));
            CalibrationUserControl.Refresh();
            BlendingUserControl.Refresh();
            _screenView.Refresh(config.Settings.ControlPointsMode, config.Settings.ShowWireframe);
            ////_screen.InitFromConfig();
            //CalibrationUserControl.Refresh();
            //_screenView.Refresh(ControlPointsMode.None, false);
            //BlendingUserControl.Refresh();
            ////_screenView.UpdateWarpControl(false, false);
            Mouse.OverrideCursor = null;
        }

        public void LoadSettings()
        {
            var filename = Path.Combine(GetProgramDataDirectory(), "PanoBeamSettings.config");
            var xmlSerializer = new XmlSerializer(typeof(Settings));
            Settings settings;
            using (var reader = new XmlTextReader(filename))
            {
                settings = (Settings)xmlSerializer.Deserialize(reader);
            }
            Configuration.Configuration.Instance.Settings.UpdateSettings(settings);
            _screen.Update(settings.PatternSize, new Size(settings.PatternCountX, settings.PatternCountY), settings.KeepCorners, settings.ControlPointsInsideOverlap);
            //_screen.InitSettingsFromConfig();
            CalibrationUserControl.Refresh();
            _screenView.Refresh(Configuration.Configuration.Instance.Settings.ControlPointsMode, Configuration.Configuration.Instance.Settings.ShowWireframe);
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(_configFilename))
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "PanoBeam Config (*.config)|*.config",
                    InitialDirectory = GetDefaultDataDirectory()
                };
                if (sfd.ShowDialog() == true)
                {
                    _configFilename = sfd.FileName;
                }
                else
                {
                    return;
                }
            }
            var xmlSerializer = new XmlSerializer(typeof(Configuration.Configuration));

            //_screen.UpdateConfig();
            var projectorsData = _screen.GetProjectorsData();
            for(var i = 0;i<projectorsData.Length;i++)
            {
                Configuration.Configuration.Instance.Projectors[i].ControlPoints = projectorsData[i].ControlPoints.Select(MapControlPoint).ToArray();
                Configuration.Configuration.Instance.Projectors[i].BlacklevelControlPoints = projectorsData[i].BlacklevelControlPoints.Select(MapControlPoint).ToArray();
                Configuration.Configuration.Instance.Projectors[i].Blacklevel2ControlPoints = projectorsData[i].Blacklevel2ControlPoints.Select(MapControlPoint).ToArray();
                Configuration.Configuration.Instance.Projectors[i].BlendRegionControlPoints = projectorsData[i].BlendRegionControlPoints.Select(MapControlPoint).ToArray();
            }
            UpdateClippingRectangleSettings();

            using (var writer = new StreamWriter(_configFilename))
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
            {
                xmlSerializer.Serialize(xmlWriter, Configuration.Configuration.Instance);
            }
        }

        private static Configuration.ControlPoint MapControlPoint(PanoBeamLib.ControlPoint controlPoint)
        {
            return new Configuration.ControlPoint
            {
                X = controlPoint.X,
                Y = controlPoint.Y,
                U = controlPoint.U,
                V = controlPoint.V,
                ControlPointType = MapControlPointType(controlPoint.ControlPointType)
            };
        }

        private static Configuration.ControlPointType MapControlPointType(PanoBeamLib.ControlPointType controlPointType)
        {
            if (controlPointType == PanoBeamLib.ControlPointType.Default)
            {
                return Configuration.ControlPointType.Default;
            }
            if (controlPointType == PanoBeamLib.ControlPointType.IsEcke)
            {
                return Configuration.ControlPointType.IsEcke;
            }
            if (controlPointType == PanoBeamLib.ControlPointType.IsFix)
            {
                return Configuration.ControlPointType.IsFix;
            }
            throw new Exception($"Unknwon ControlPointType {controlPointType}");
        }

        private void SaveSettings()
        {
            var filename = Path.Combine(GetProgramDataDirectory(), "PanoBeamSettings.config");
            var xmlSerializer = new XmlSerializer(typeof(Settings));

            UpdateClippingRectangleSettings();

            using (var writer = new StreamWriter(filename))
            using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
            {
                xmlSerializer.Serialize(xmlWriter, Configuration.Configuration.Instance.Settings);
            }
        }

        private void UpdateClippingRectangleSettings()
        {
            var clippingRectangle = new SimpleRectangle(CameraUserControl.GetClippingRectangle());
            if (clippingRectangle.Width > 0 && clippingRectangle.Height > 0)
            {
                Configuration.Configuration.Instance.Settings.ClippingRectangle = clippingRectangle;
            }
        }

        //public void SaveSettings(CalibrationData calibrationData)
        //{
        //    var filename = Path.Combine(GetProgramDataDirectory(), "PanoBeamSettings.config");
        //    var xmlSerializer = new XmlSerializer(typeof(Settings));

        //    //_screen.UpdateSettings();
        //    var clippingRectangle = new SimpleRectangle(CameraUserControl.GetClippingRectangle());
        //    if (clippingRectangle.Width > 0 && clippingRectangle.Height > 0)
        //    {
        //        Configuration.Configuration.Instance.Settings.ClippingRectangle = clippingRectangle;
        //    }
        //    Configuration.Configuration.Instance.Settings.PatternSize = calibrationData.PatternSize;
        //    Configuration.Configuration.Instance.Settings.PatternCountX = calibrationData.PatternCount.Width;
        //    Configuration.Configuration.Instance.Settings.PatternCountY = calibrationData.PatternCount.Height;
        //    Configuration.Configuration.Instance.Settings.KeepCorners = calibrationData.KeepCorners;

        //    using (var writer = new StreamWriter(filename))
        //    using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Indent = true }))
        //    {
        //        xmlSerializer.Serialize(xmlWriter, Configuration.Configuration.Instance.Settings);
        //    }
        //}
    }
}