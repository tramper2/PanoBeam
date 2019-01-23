using System;

namespace PanoBeam.Events
{
    class EventSubscription<TPayload> : IEventSubscription
    {
        protected Action<TPayload> Action { get; }

        public EventSubscription(Action<TPayload> action)
        {
            Action = action;
        }

        public virtual void InvokeAction(TPayload argument)
        {
            Action(argument);
        }

        public void Execute(object[] arguments)
        {
            InvokeAction((TPayload)arguments[0]);
        }
    }
}