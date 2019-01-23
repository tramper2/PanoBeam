using System.Drawing;

namespace PanoBeam
{
    public static class Extensions
    {
        public static Rectangle GetRectangle(this System.Windows.Rect rectangle)
        {
            return new Rectangle((int)rectangle.X, (int)rectangle.Y, (int)rectangle.Width, (int)rectangle.Height);
        }
    }
}