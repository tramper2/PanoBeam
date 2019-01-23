using Size = System.Drawing.Size;

namespace PanoBeam.Controls
{
    public delegate void CalibrationStartDelegate(int patternSize, Size patternCount, bool keepCorners);
    /// <summary>
    /// Interaction logic for CalibrationUserControl.xaml
    /// </summary>
    public partial class CalibrationUserControl
    {
        public event CalibrationStartDelegate Start;
        private readonly CalibrationUserControlViewModel _viewModel;

        public int PatternSize => _viewModel.PatternSize;

        public Size PatternCount => _viewModel.PatternCount;

        //private Screen _screen;

        public void SetInProgress(bool value)
        {
            _viewModel.SetInProgress(value);
        }

        public CalibrationUserControl()
        {
            InitializeComponent();
            _viewModel = new CalibrationUserControlViewModel {StartAction = RaiseStart};
            DataContext = _viewModel;

            //ControlPointsChanged?.Invoke(_viewModel.GetControlPointsData());
            //Loaded += (sender, args) =>
            //{
            //    var w = Window.GetWindow(this);
            //};
        }

        public void Initialize()
        {
            //_screen = screen;
        }

        public void Refresh()
        {
            _viewModel.PatternSize = Configuration.Configuration.Instance.Settings.PatternSize;
            _viewModel.ControlPointsCountX = Configuration.Configuration.Instance.Settings.PatternCountX;
            _viewModel.ControlPointsCountY = Configuration.Configuration.Instance.Settings.PatternCountY;
            _viewModel.KeepCorners = Configuration.Configuration.Instance.Settings.KeepCorners;
            _viewModel.ControlPointsMode = Configuration.Configuration.Instance.Settings.ControlPointsMode;
            _viewModel.ShowWireframe = Configuration.Configuration.Instance.Settings.ShowWireframe;
            _viewModel.ControlPointsInsideOverlap = Configuration.Configuration.Instance.Settings.ControlPointsInsideOverlap;
            _viewModel.ImmediateWarp = Configuration.Configuration.Instance.Settings.ImmediateWarp;
        }

        private void RaiseStart()
        {
            Start?.Invoke(_viewModel.PatternSize, _viewModel.PatternCount, _viewModel.KeepCorners);
        }
    }
}
