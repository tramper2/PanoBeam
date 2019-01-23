namespace PanoBeamLib
{
    public class ProjectorData
    {
        public BlendSettings BlendData { get; set; }

        public ControlPoint[] ControlPoints { get; set; }

        public ControlPoint[] BlacklevelControlPoints { get; set; }

        public ControlPoint[] Blacklevel2ControlPoints { get; set; }

        public ControlPoint[] BlendRegionControlPoints { get; set; }

        public ProjectorData()
        {
            BlendData = new BlendSettings();
        }
    }
}
