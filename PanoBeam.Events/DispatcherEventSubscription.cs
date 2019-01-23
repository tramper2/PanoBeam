using System;

namespace PanoBeam.Events
{
    class DispatcherEventSubscription<TPayload> : EventSubscription<TPayload>
    {
        private readonly IDispatcher _dispatcher;

        public DispatcherEventSubscription(Action<TPayload> action, IDispatcher dispatcher)
            : base(action)
        {
            _dispatcher = dispatcher;
        }

        public override void InvokeAction(TPayload argument)
        {
            _dispatcher.BeginInvoke(Action, argument);
        }
    }
}