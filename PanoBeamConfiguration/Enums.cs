namespace PanoBeam.Configuration
{
    public enum CurvePointType
    {
        Line,
        Spline
    }

    public enum ControlPointType
    {
        Default,
        IsFix,
        IsEcke
    }

    public enum ControlPointsMode
    {
        None,
        Calibration,
        Blacklevel,
        Blacklevel2,
        Blendregion
    }
}