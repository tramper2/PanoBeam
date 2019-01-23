using System.Xml.Serialization;

namespace PanoBeam.Configuration
{
    public class ControlPoint
    {
        [XmlAttribute("X")]
        public int X { get; set; }

        [XmlAttribute("Y")]
        public int Y { get; set; }

        [XmlAttribute("U")]
        public int U { get; set; }

        [XmlAttribute("V")]
        public int V { get; set; }

        [XmlAttribute("Type")]
        public ControlPointType ControlPointType { get; set; }
    }
}