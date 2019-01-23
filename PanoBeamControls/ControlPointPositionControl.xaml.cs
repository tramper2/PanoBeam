using System.Windows;
using System.Windows.Controls;

namespace PanoBeam.Controls
{
    /// <summary>
    /// Interaction logic for ControlPointPosition.xaml
    /// </summary>
    public partial class ControlPointPositionControl
    {
        public ControlPointPositionControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty XProperty = DependencyProperty.Register(
            "X", typeof(int), typeof(ControlPointPositionControl), new PropertyMetadata(default(int)));

        public int X
        {
            get => (int)Canvas.GetLeft(this);
            set
            {
                SetValue(XProperty, value);
                Canvas.SetLeft(this, value);
            }
        }

        public static readonly DependencyProperty YProperty = DependencyProperty.Register(
            "Y", typeof(int), typeof(ControlPointPositionControl), new PropertyMetadata(default(int)));

        public int Y
        {
            get => (int)Canvas.GetTop(this);
            set
            {
                SetValue(YProperty, value);
                Canvas.SetTop(this, value);
            }
        }
    }
}
