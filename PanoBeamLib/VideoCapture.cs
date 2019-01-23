using System;
using System.Drawing;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video.DirectShow;

namespace PanoBeamLib
{
    public class VideoCapture
    {
        private VideoCaptureDevice _videoDevice;
        private VideoCapabilities[] _videoCapabilities;

        public Action<BitmapSource, int, int> FirstFrame;
        public Action<BitmapSource> Frame;
        public Action<Bitmap> SaveFrame;

        public Rect ClippingRectangle { get; set; }

        private string _monikerString;

        private static readonly Lazy<VideoCapture> Lazy = new Lazy<VideoCapture>(() => new VideoCapture());

        private Timer _timer;


        public static VideoCapture Instance => Lazy.Value;

        private VideoCapture()
        {
        }

        public void SetCamera(string monikerString)
        {
            _monikerString = monikerString;
            SelectVideoDevice(monikerString);
        }

        public void Start()
        {
            // TODO Marco: Kamera oder File
            if(Helpers.CameraCalibration)
            {
                Start(false);
            }
            else
            {
                StartFromFile(false);
            }
        }

        internal void StartFromFile(bool background)
        {
            var bmp = (Bitmap)Image.FromFile(@"C:\source\PanoBeam\src\PanoBeam\Calibration\6x5\capture_white.png");
            _timer = new Timer(200);
            if (background)
            {
                _timer.Elapsed += (sender, args) =>
                {
                    SaveFrame?.Invoke(bmp);
                };
            }
            else
            {
                FirstFrame(bmp.GetBitmapSource(), bmp.Width, bmp.Height);
                _timer.Elapsed += (sender, args) =>
                {
                    ProcessBitmap(bmp);
                };
            }
            _timer.Start();
            //bmp.Dispose();
        }

        public void Start(bool background)
        {
            if (_videoDevice == null)
            {
                SetCamera(_monikerString);
            }
            if (_videoCapabilities == null) return;
            if (_videoCapabilities.Length == 0) return;

            // ReSharper disable once PossibleNullReferenceException
            _videoDevice.VideoResolution = GetMaxResolution();

            if (background)
            {
                _videoDevice.NewFrame += VideoDevice_NewFrameBackground;
            }
            else
            {
                _videoDevice.NewFrame += FirstFrameEvent;
            }

            _videoDevice.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            SaveFrame = null;
            if (_videoDevice == null) return;
            _videoDevice.SignalToStop();
            _videoDevice.NewFrame -= FirstFrameEvent;
            _videoDevice.NewFrame -= VideoDevice_NewFrame;
            _videoDevice = null;
        }

        public void ShowCameraSettings(IntPtr parentWindow)
        {
            _videoDevice?.DisplayPropertyPage(parentWindow);
        }

        private void FirstFrameEvent(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            _videoDevice.NewFrame -= FirstFrameEvent;
            var bmp = (Bitmap)eventArgs.Frame.Clone();
            FirstFrame(bmp.GetBitmapSource(), bmp.Width, bmp.Height);
            bmp.Dispose();

            _videoDevice.NewFrame += VideoDevice_NewFrame;
        }

        private void VideoDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            var bmp = (Bitmap)eventArgs.Frame.Clone();
            ProcessBitmap(bmp);
        }

        private void ProcessBitmap(Bitmap bmp)
        {
            //var fast = new FastCornersDetector
            //{
            //    Threshold = 20,
            //    Suppress = true
            //};
            //var corners = new RectangleCornersMarker(fast, Color.Red);
            //corners.Rectangle = ClippingRectangle;
            //bmp = corners.Apply(bmp);
            Frame(bmp.GetBitmapSource());
            bmp.Dispose();
        }

        private void VideoDevice_NewFrameBackground(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            SaveFrame?.Invoke(eventArgs.Frame);
        }

        private void SelectVideoDevice(string monikerString)
        {
            _videoDevice = new VideoCaptureDevice(monikerString);
            _videoCapabilities = _videoDevice.VideoCapabilities.Where(c => c.FrameSize.Width <= 1920).ToArray();
        }

        private VideoCapabilities GetMaxResolution()
        {
            return _videoCapabilities.OrderByDescending(v => v.FrameSize.Width).ThenByDescending(v => v.FrameSize.Height).First();
        }
    }
}