using PanoBeamLib;

namespace PanoBeam.Controls
{
    /// <summary>
    /// Interaction logic for BlendingUserControl.xaml
    /// </summary>
    public partial class BlendingUserControl
    {
        public BlendingUserControl()
        {
            InitializeComponent();
        }

        public void Initialize(Projector[] projectors)
        {
            foreach (var p in projectors)
            {
                var pc = new BlendControls.ProjectorControl();
                pc.Initialize(p);
                TheContent.Children.Add(pc);
            }
        }

        public void Refresh()
        {
            foreach (var child in TheContent.Children)
            {
                var control = child as BlendControls.ProjectorControl;
                control?.Refresh();
            }
        }
    }
}
