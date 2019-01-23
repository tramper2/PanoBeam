using System;

namespace PanoBeam.Common
{
    [Flags]
    public enum ControlPointDirections
    {
        None = 0,
        Up = 1,
        Right = 2,
        Down = 4,
        Left = 8
    }
}
