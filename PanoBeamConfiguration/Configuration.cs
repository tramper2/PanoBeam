using System;

namespace PanoBeam.Configuration
{
    public sealed class Configuration
    {
        public Settings Settings { get; set; }
        
        public Projector[] Projectors { get; set; }

        private static readonly Lazy<Configuration> Lazy = new Lazy<Configuration>(() => new Configuration());

        public static Configuration Instance => Lazy.Value;

        private Configuration()
        {
            Settings = new Settings();
            var p0 = new Projector
            {
                BlendData =
                {
                    MaxBlend = 1,
                    BlacklevelOffset = 0.02d,
                    Blacklevel2Offset = 0,
                    CurvePoints = new[]
                    {
                        new CurvePoint {X = 0, Y = 1, Type = CurvePointType.Line},
                        new CurvePoint {X = 0.64, Y = 0.7352, Type = CurvePointType.Spline},
                        new CurvePoint {X = 0.905, Y = 0.2602, Type = CurvePointType.Spline},
                        new CurvePoint {X = 1, Y = 0, Type = CurvePointType.Line},
                    }
                }
            };
            var p1 = new Projector
            {
                BlendData =
                {
                    MaxBlend = 0.94666666d,
                    BlacklevelOffset = 0.012d,
                    Blacklevel2Offset = 0,
                    CurvePoints = new[]
                    {
                        new CurvePoint {X = 0, Y = 1, Type = CurvePointType.Line},
                        new CurvePoint {X = 0.12, Y = 0.9652, Type = CurvePointType.Spline},
                        new CurvePoint {X = 0.24, Y = 0.8902, Type = CurvePointType.Spline},
                        new CurvePoint {X = 0.375, Y = 0.7802, Type = CurvePointType.Spline},
                        new CurvePoint {X = 0.62, Y = 0.4801999995, Type = CurvePointType.Spline},
                        new CurvePoint {X = 0.935, Y = 0.0602, Type = CurvePointType.Spline},
                        new CurvePoint {X = 1, Y = 0, Type = CurvePointType.Line},
                    }
                }
            };
            Projectors = new[] { p0, p1 };
        }

        public void UpdateConfig(Configuration config)
        {
            Instance.Projectors = config.Projectors;
            Instance.Settings.UpdateSettings(config.Settings);
        }
    }
}
