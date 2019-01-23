using System;
using System.Collections.Generic;

namespace PanoBeam.Events
{
    public sealed class EventAggregator
    {
        #region singleton
        private static volatile EventAggregator _instance;
        private static readonly object SyncRoot = new object();

        private EventAggregator() { }

        public static EventAggregator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                            _instance = new EventAggregator();
                    }
                }

                return _instance;
            }
        }
        #endregion

        private readonly Dictionary<Type, EventBase> _events = new Dictionary<Type, EventBase>();

        public TEventType GetEvent<TEventType>() where TEventType : EventBase, new()
        {
            if (_events.TryGetValue(typeof(TEventType), out var existingEvent))
            {
                return (TEventType)existingEvent;
            }

            var newEvent = new TEventType();
            _events[typeof(TEventType)] = newEvent;
            return newEvent;
        }
    }
}