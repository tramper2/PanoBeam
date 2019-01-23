using PanoBeamLib;

namespace PanoBeam.BlendControls
{
    /// <summary>
    /// Interaction logic for ProjectorControl.xaml
    /// </summary>
    public partial class ProjectorControl
    {
        private Projector _projector;
        private ProjectorViewModel _viewModel;

        public ProjectorControl()
        {
            InitializeComponent();
        }
        
        public void Initialize(Projector projector)
        {
            _projector = projector;
            _viewModel = new ProjectorViewModel(projector);
            DataContext = _viewModel;
            CurveControl1.SetBlendCurve(projector.BlendCurve);
        }

        public void Refresh()
        {
            _viewModel.MaxBlend = _projector.MaxBlend;
            _viewModel.BlacklevelOffset = _projector.BlacklevelOffset;
            _viewModel.Blacklevel2Offset = _projector.Blacklevel2Offset;
            CurveControl1.Refresh();
        }
    }
}
