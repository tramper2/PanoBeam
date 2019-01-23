using System.Windows;
using System.Xml.Serialization;

namespace PanoBeam.Configuration
{
    [XmlRoot(ElementName = "Rect")]
    public class SimpleRectangle
    {
        public Point Location { get; set; }
        public Size Size { get; set; }

        public SimpleRectangle() { }

        public SimpleRectangle(Point location, Size size)
        {
            Location = location;
            Size = size;
        }

        public SimpleRectangle(Rect rect)
        {
            Location = rect.Location;
            Size = rect.Size;
        }

        [XmlIgnore]
        public double X => Location.X;

        [XmlIgnore]
        public double Y => Location.Y;

        [XmlIgnore]
        public double Width => Size.Width;

        [XmlIgnore]
        public double Height => Size.Height;
    }
}
