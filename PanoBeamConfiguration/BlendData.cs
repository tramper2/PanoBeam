namespace PanoBeam.Configuration
{
    public class BlendData
    {
        public double MaxBlend { get; set; }

        public double BlacklevelOffset { get; set; }

        public double Blacklevel2Offset { get; set; }

        public CurvePoint[] CurvePoints { get; set; }
    }
}