using System;
using System.Threading;

namespace PanoBeam.Events
{
    class BackgroundEventSubscription<TPayload> : EventSubscription<TPayload>
    {
        public BackgroundEventSubscription(Action<TPayload> action) : base(action) { }

        public override void InvokeAction(TPayload argument)
        {
            ThreadPool.QueueUserWorkItem((o) => Action(argument));
        }
    }
}