namespace PanoBeamLib.Blend
{
    public class ControlPoint
    {
        public double X { get; private set; }

        public double Y { get; private set; }

        public ControlPointType PointType { get; set; }

        public ControlPoint(double x, double y, ControlPointType pointType)
        {
            X = x;
            Y = y;
            PointType = pointType;
        }

        public void Update(double x, double y)
        {
            X = x;
            Y = y;
        }

        public ControlPoint NeighborLeft { get; set; }
        public ControlPoint NeighborRight { get; set; }
    }
}