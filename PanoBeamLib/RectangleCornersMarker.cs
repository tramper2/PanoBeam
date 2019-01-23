using System.Drawing;
using System.Windows;
using System.Collections.Generic;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace PanoBeamLib
{
    public class RectangleCornersMarker : CornersMarker
    {
        public RectangleCornersMarker(ICornersDetector detector, Color markerColor) : base(detector, markerColor)
        {
            CornerSize = 3;
        }

        public Rect Rectangle { get; set; }

        public int CornerSize { get; set; }

        protected override void ProcessFilter(UnmanagedImage image)
        {
            int d = CornerSize / 2;
            // get collection of corners
            List<IntPoint> corners = Detector.ProcessImage(image);
            // mark all corners
            foreach (IntPoint corner in corners)
            {
                if (Rectangle.Contains(corner.X, corner.Y))
                {
                    Drawing.FillRectangle(image, new Rectangle(corner.X - d, corner.Y - d, CornerSize, CornerSize), MarkerColor);
                }
            }
        }
    }
}
