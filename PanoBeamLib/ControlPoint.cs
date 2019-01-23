using PanoBeam.Common;

namespace PanoBeamLib
{
    public class ControlPoint
    {
        // U und V sind die ursprünglichen Werte, X und Y die verschobenen
        public int X { get; set; }

        public int Y { get; set; }

        public int U { get; set; }

        public int V { get; set; }

        public ControlPointType ControlPointType { get; set; }

        public ControlPointDirections ControlPointDirections { get; set; }

        public void AllowAllDirections()
        {
            ControlPointDirections = ControlPointDirections.Up | ControlPointDirections.Right | ControlPointDirections.Down | ControlPointDirections.Left;
        }

        internal ControlPoint AssociatedPoint { get; set; }

        internal PatternShape PatternShape { get; set; }

        internal Shape DetectedShape { get; set; }
    }
}