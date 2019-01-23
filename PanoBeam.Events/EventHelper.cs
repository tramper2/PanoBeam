using System;

namespace PanoBeam.Events
{
    public class EventHelper
    {
        public static void SendEvent<TEventType, TPayload>(TPayload payload) where TEventType : Event<TPayload>, new()
        {
            EventAggregator.Instance.GetEvent<TEventType>().Publish(payload);
        }

        public static void SubscribeEvent<TEventType, TPayload>(Action<TPayload> action, bool keepSubscriberReferenceAlive) where TEventType : Event<TPayload>, new()
        {
            SubscribeEvent<TEventType, TPayload>(action, ThreadOption.None, keepSubscriberReferenceAlive);
        }

        public static void SubscribeEvent<TEventType, TPayload>(Action<TPayload> action) where TEventType : Event<TPayload>, new()
        {
            SubscribeEvent<TEventType, TPayload>(action, ThreadOption.None);
        }

        public static void SubscribeEvent<TEventType, TPayload>(Action<TPayload> action, ThreadOption threadOption) where TEventType : Event<TPayload>, new()
        {
            SubscribeEvent<TEventType, TPayload>(action, threadOption, false);
        }

        public static void SubscribeEvent<TEventType, TPayload>(Action<TPayload> action, ThreadOption threadOption, bool keepSubscriberReferenceAlive) where TEventType : Event<TPayload>, new()
        {
            EventAggregator.Instance.GetEvent<TEventType>().Subscribe(action, threadOption, keepSubscriberReferenceAlive);
        }
    }
}