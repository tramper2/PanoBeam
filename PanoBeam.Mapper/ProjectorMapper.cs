using System;
using System.Linq;
using PanoBeamLib;

namespace PanoBeam.Mapper
{
    public class ProjectorMapper
    {
        public static ProjectorData[] MapProjectorsData(Configuration.Projector[] projectors)
        {
            return projectors?.Select(MapProjectorData).ToArray();
        }

        private static ProjectorData MapProjectorData(Configuration.Projector projector)
        {
            return new ProjectorData
            {
                BlendData = new BlendSettings
                {
                    MaxBlend = projector.BlendData.MaxBlend,
                    BlacklevelOffset = projector.BlendData.BlacklevelOffset,
                    Blacklevel2Offset = projector.BlendData.Blacklevel2Offset,
                    CurvePoints = projector.BlendData.CurvePoints.Select(MapCurvePoint).ToArray()
                },
                ControlPoints = projector.ControlPoints?.Select(MapControlPoint).ToArray(),
                BlendRegionControlPoints = projector.BlendRegionControlPoints?.Select(MapControlPoint).ToArray(),
                BlacklevelControlPoints = projector.BlacklevelControlPoints?.Select(MapControlPoint).ToArray(),
                Blacklevel2ControlPoints = projector.Blacklevel2ControlPoints?.Select(MapControlPoint).ToArray()
            };
        }

        private static ControlPoint MapControlPoint(Configuration.ControlPoint controlPoint)
        {
            return new ControlPoint
            {
                X = controlPoint.X,
                Y = controlPoint.Y,
                U = controlPoint.U,
                V = controlPoint.V,
                ControlPointType = MapControlPointType(controlPoint.ControlPointType)
            };
        }

        private static ControlPointType MapControlPointType(Configuration.ControlPointType controlPointType)
        {
            if (controlPointType == Configuration.ControlPointType.Default) return ControlPointType.Default;
            if (controlPointType == Configuration.ControlPointType.IsEcke) return ControlPointType.IsEcke;
            if (controlPointType == Configuration.ControlPointType.IsFix) return ControlPointType.IsFix;
            throw new Exception($"Unknown ControlPointType {controlPointType}");
        }

        private static CurvePoint MapCurvePoint(Configuration.CurvePoint curvePoint)
        {
            return new CurvePoint
            {
                Type = MapCurvePointType(curvePoint.Type),
                X = curvePoint.X,
                Y = curvePoint.Y
            };
        }

        private static CurvePointType MapCurvePointType(Configuration.CurvePointType curvePointType)
        {
            if (curvePointType == Configuration.CurvePointType.Line) return CurvePointType.Line;
            if (curvePointType == Configuration.CurvePointType.Spline) return CurvePointType.Spline;
            throw new Exception($"Unknown CurvePointType {curvePointType}");
        }
    }
}
