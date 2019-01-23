using PanoBeamLib;

namespace PanoBeam.BlendControls
{
    public class ProjectorViewModel : BindableBase
    {
        public Projector Projector { get; }

        private double _maxBlend;
        private double _blacklevelOffsetOffset;
        private double _blacklevel2OffsetOffset;

        public double MaxBlend
        {
            get => _maxBlend;
            set
            {
                SetProperty(ref _maxBlend, value);
                Projector.MaxBlend = value;
            }
        }

        public double BlacklevelOffset
        {
            get => _blacklevelOffsetOffset;
            set
            {
                SetProperty(ref _blacklevelOffsetOffset, value);
                Projector.BlacklevelOffset = value;
            }
        }

        public double Blacklevel2Offset
        {
            get => _blacklevel2OffsetOffset;
            set
            {
                SetProperty(ref _blacklevel2OffsetOffset, value);
                Projector.Blacklevel2Offset = value;
            }
        }

        public ProjectorViewModel(Projector projector)
        {
            _maxBlend = projector.MaxBlend;
            _blacklevelOffsetOffset = projector.BlacklevelOffset;
            _blacklevel2OffsetOffset = projector.Blacklevel2Offset;
            Projector = projector;
        }
    }
}