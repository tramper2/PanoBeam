using System.Windows;

namespace PanoBeam.Configuration
{
    public class Settings
    {
        public Camera Camera { get; set; }

        public SimpleRectangle ClippingRectangle { get; set; }

        public int PatternSize { get; set; }

        public int PatternCountX { get; set; }

        public int PatternCountY { get; set; }

        public ControlPointsMode ControlPointsMode { get; set; }

        public bool ShowWireframe { get; set; }

        public bool ControlPointsInsideOverlap { get; set; }

        public bool KeepCorners { get; set; }

        public bool ImmediateWarp { get; set; }        

        public Settings()
        {
            Camera = new Camera
            {
                MonikerString = "@device:pnp:\\\\?\\usb#vid_046d&pid_082d&mi_00#7&3158886&0&0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\\{bbefb6c7-2fc4-4139-bb8b-a58bba724083}"
            };
            ClippingRectangle = new SimpleRectangle(new Point(76.1, 248.6), new Size(1741.6, 615.0));
            PatternSize = 80;
            PatternCountX = 10;
            PatternCountY = 7;
            ControlPointsMode = ControlPointsMode.None;

            ShowWireframe = false;
            ControlPointsInsideOverlap = false;
            KeepCorners = true;
            ImmediateWarp = false;
        }

        public void UpdateSettings(Settings settings)
        {
            PatternSize = settings.PatternSize;
            PatternCountX = settings.PatternCountX;
            PatternCountY = settings.PatternCountY;
            ClippingRectangle = settings.ClippingRectangle;
            ControlPointsMode = settings.ControlPointsMode;

            ShowWireframe = settings.ShowWireframe;
            ControlPointsInsideOverlap = settings.ControlPointsInsideOverlap;
            KeepCorners = settings.KeepCorners;
            ImmediateWarp = settings.ImmediateWarp;
        }
    }
}
