using System;

namespace PanoBeamLib
{
    internal class PatternShape
    {
        public int X { get; private set; }
        public int Y { get; }
        public int W { get; private set; }
        public int H { get; }

        public PatternShape(int x, int y, int w, int h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public int CenterX => X + (int) Math.Round(W/2f, MidpointRounding.AwayFromZero);
        public int CenterY => Y + (int) Math.Round(H/2f, MidpointRounding.AwayFromZero);

        public void ShrinkToWidth(int width, int xOffset)
        {
            if (width < 0)
            {
                X += xOffset;
                W = -width;
            }
            else
            {
                X += W - width + xOffset;
                W = width;
            }
        }
    }
}