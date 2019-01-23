using System.Drawing;
using System.Xml;
using System.Xml.Serialization;
using PanoBeam.Mapper;
using PanoBeamLib;

namespace PanoBeam
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run(args[0]);
        }

        public void Run(string configFile)
        {
            PanoScreen.Initialize();
            var mosaicInfo = PanoScreen.GetMosaicInfo();

            var screen = new PanoScreen
            {
                Resolution = new Size((int)mosaicInfo.ProjectorWidth * 2 - mosaicInfo.Overlap, (int)mosaicInfo.ProjectorHeight),
                Overlap = mosaicInfo.Overlap
            };
            screen.AddProjectors(mosaicInfo.DisplayId0, mosaicInfo.DisplayId1);

            var xmlSerializer = new XmlSerializer(typeof(Configuration.Configuration));
            Configuration.Configuration config;
            using (var reader = new XmlTextReader(configFile))
            {
                config = (Configuration.Configuration)xmlSerializer.Deserialize(reader);
            }
            Configuration.Configuration.Instance.UpdateConfig(config);
            screen.Update(config.Settings.PatternSize, new Size(config.Settings.PatternCountX, config.Settings.PatternCountY), config.Settings.KeepCorners, config.Settings.ControlPointsInsideOverlap);
            screen.UpdateProjectorsFromConfig(ProjectorMapper.MapProjectorsData(Configuration.Configuration.Instance.Projectors));

            screen.WarpBlend(false);
            //screen.Warp();
        }
    }
}
