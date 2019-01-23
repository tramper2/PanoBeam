using AForge;
using AForge.Imaging;
using System.Drawing;

namespace PanoBeamLib
{
    internal class Shape
    {
        public IntPoint[] Corners { get; set; }

        internal PointF TransformedPoint { get; set; }

        public Blob Blob { get; set; }

        public Shape(IntPoint[] corners, Blob blob)
        {
            Corners = corners;
            Blob = blob;
        }
    }
}