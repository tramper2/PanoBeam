using System.Xml.Serialization;

namespace PanoBeam.Configuration
{
    public class Projector
    {
        public BlendData BlendData { get; set; }

        public ControlPoint[] ControlPoints { get; set; }
        
        public ControlPoint[] BlacklevelControlPoints { get; set; }

        [XmlIgnore]
        public ControlPoint[] Blacklevel2ControlPoints { get; set; }

        [XmlIgnore]
        public ControlPoint[] BlendRegionControlPoints { get; set; }

        public Projector()
        {
            BlendData = new BlendData();
        }
    }
}