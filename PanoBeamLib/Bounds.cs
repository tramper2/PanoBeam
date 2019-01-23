using System.Drawing;

namespace PanoBeamLib
{
    public class Bounds
    {
        public int X1 { get; }
        public int X2 { get; }
        public int Y1 { get; }
        public int Y2 { get; }

        public Bounds(int x1, int y1, int x2, int y2)
        {
            X1 = x1;
            X2 = x2;
            Y1 = y1;
            Y2 = y2;
        }

        public Rectangle ToRectangle()
        {
            return new Rectangle(X1, Y1, X2 - X1, Y2 - Y1);
        }
    }
}