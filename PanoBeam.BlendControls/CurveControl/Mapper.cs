using System;
using PanoBeam.BlendControls.CurveControl.Enums;

namespace PanoBeam.BlendControls.CurveControl
{
    public class Mapper
    {
        public static PanoBeamLib.Blend.ControlPointType ConvertControlPointType(ControlPointType type)
        {
            if (type == ControlPointType.Line)
            {
                return PanoBeamLib.Blend.ControlPointType.Line;
            }
            if (type == ControlPointType.Spline)
            {
                return PanoBeamLib.Blend.ControlPointType.Spline;
            }
            throw new Exception($"Unknown ControlPointType {type}");
        }

        public static ControlPointType ConvertControlPointType(PanoBeamLib.Blend.ControlPointType type)
        {
            if (type == PanoBeamLib.Blend.ControlPointType.Line)
            {
                return ControlPointType.Line;
            }
            if (type == PanoBeamLib.Blend.ControlPointType.Spline)
            {
                return ControlPointType.Spline;
            }
            throw new Exception($"Unknown ControlPointType {type}");
        }
    }
}