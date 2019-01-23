using System;

namespace PanoBeam.Events
{
    interface IDispatcher
    {
        void BeginInvoke(Delegate method, object arg);
    }
}