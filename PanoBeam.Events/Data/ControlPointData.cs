namespace PanoBeam.Events.Data
{
    public class ControlPointData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int U { get; set; }
        public int V { get; set; }

        public ControlPointData(int x, int y, int u, int v)
        {
            X = x;
            Y = y;
            U = u;
            V = v;
        }
    }
}