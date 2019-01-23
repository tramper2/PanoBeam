using System;
using System.Windows;
using System.Windows.Threading;

namespace PanoBeam.Events
{
    // ReSharper disable once InconsistentNaming
    public class UIDispatcher : IDispatcher
    {
        public void BeginInvoke(Delegate method, object arg)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, method, arg);
            }
        }
    }
}