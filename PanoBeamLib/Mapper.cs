using System;

namespace PanoBeamLib
{
    internal class Mapper
    {
        public static Blend.ControlPointType MapCurvePointType(CurvePointType type)
        {
            if(type == CurvePointType.Line) return Blend.ControlPointType.Line;
            if (type == CurvePointType.Spline) return Blend.ControlPointType.Spline;
            throw new Exception($"Unknown CurvePointType {type}");
        }

        public static CurvePointType MapControlPointType(Blend.ControlPointType type)
        {
            if (type == Blend.ControlPointType.Line) return CurvePointType.Line;
            if (type == Blend.ControlPointType.Spline) return CurvePointType.Spline;
            throw new Exception($"Unknown ControlPointType {type}");
        }
    }
}