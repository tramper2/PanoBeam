using System;
using System.Collections.Generic;

namespace PanoBeam.Events
{
    public class Event<TPayload> : EventBase
    {
        private readonly List<IEventSubscription> _subscriptions = new List<IEventSubscription>();

        private readonly IDispatcher _uiDispatcher = new UIDispatcher();

        public void Subscribe(Action<TPayload> action, bool keepSubscriberReferenceAlive)
        {
            Subscribe(action, ThreadOption.None, keepSubscriberReferenceAlive);
        }

        public void Subscribe(Action<TPayload> action, ThreadOption threadOption)
        {
            Subscribe(action, threadOption, false);
        }

        public void Subscribe(Action<TPayload> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive)
        {
            EventSubscription<TPayload> subscription;

            if (threadOption == ThreadOption.None)
            {
                subscription = new EventSubscription<TPayload>(action);
            }
            else if (threadOption == ThreadOption.UIThread)
            {
                subscription = new DispatcherEventSubscription<TPayload>(action, _uiDispatcher);
            }
            else if (threadOption == ThreadOption.BackgroundThread)
            {
                subscription = new BackgroundEventSubscription<TPayload>(action);
            }
            else
            {
                subscription = new EventSubscription<TPayload>(action);
            }

            lock (_subscriptions)
            {
                _subscriptions.Add(subscription);
            }
        }

        public void Publish(TPayload payload)
        {
            InternalPublish(payload);
        }

        private void InternalPublish(params object[] arguments)
        {
            lock (_subscriptions)
            {
                foreach (var subscription in _subscriptions)
                {
                    subscription.Execute(arguments);
                }
            }
        }
    }
}