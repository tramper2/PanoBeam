using PanoBeamLib;

namespace PanoBeam.Controls
{
    public class ProjectorContainer
    {
        public ControlPointsControl.ControlPointsControl ProjectorControl { get; set; }
        public ControlPointsControl.ControlPointsControl BlacklevelControl { get; set; }
        public ControlPointsControl.ControlPointsControl Blacklevel2Control { get; set; }
        public ControlPointsControl.ControlPointsControl BlendRegionControl { get; set; }
        public Projector Projector { get; set; }
    }
}