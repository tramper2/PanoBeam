using PanoBeam.Common;

namespace PanoBeam.Controls.ControlPointsControl
{
    public class ControlPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int U { get; set; }
        public int V { get; set; }

        public ControlPointType ControlPointType { get; set; }

        public ControlPointDirections ControlPointDirections { get; set; }

        public ControlPoint(int x, int y, int u, int v, ControlPointType controlPointType, ControlPointDirections controlPointDirections)
        {
            X = x;
            Y = y;
            U = u;
            V = v;
            ControlPointType = controlPointType;
            ControlPointDirections = controlPointDirections;
        }
    }
}