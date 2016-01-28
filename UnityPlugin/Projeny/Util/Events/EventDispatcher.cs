using System;
using System.Collections.Generic;

namespace ModestTree.Util
{
    public interface IEventInvoker<TEventType>
    {
        void Invoke<T1, T2, T3, T4>(TEventType eventType, T1 p1, T2 p2, T3 p3, T4 p4);
        void Invoke<T1, T2, T3>(TEventType eventType, T1 p1, T2 p2, T3 p3);
        void Invoke<T1, T2>(TEventType eventType, T1 p1, T2 p2);
        void Invoke<T>(TEventType eventType, T p1);
        void Invoke(TEventType eventType);
    }

    public interface IEventListener<TEventType>
    {
        void Subscribe<T1, T2, T3, T4>(TEventType eventType, Action<T1, T2, T3, T4> eventDelegate);
        void Subscribe<T1, T2, T3>(TEventType eventType, Action<T1, T2, T3> eventDelegate);
        void Subscribe<T1, T2>(TEventType eventType, Action<T1, T2> eventDelegate);
        void Subscribe<T>(TEventType eventType, Action<T> eventDelegate);
        void Subscribe(TEventType eventType, Action eventDelegate);

        void Unsubscribe<T1, T2, T3>(TEventType eventType, Action<T1, T2, T3> eventDelegate);
        void Unsubscribe<T1, T2>(TEventType eventType, Action<T1, T2> eventDelegate);
        void Unsubscribe<T>(TEventType eventType, Action<T> eventDelegate);
        void Unsubscribe(TEventType eventType, Action eventDelegate);
    }

    // Generic Event System keyable by any type (though usually its an enum)
    public class EventDispatcher<TEventType> : IEventListener<TEventType>, IEventInvoker<TEventType>
    {
        Dictionary<TEventType, IEventWrapper> _wrappers = new Dictionary<TEventType, IEventWrapper>();

        ////////////////////////// Register Methods

        public void RegisterEvent(TEventType eventType)
        {
            Assert.That(!_wrappers.ContainsKey(eventType));
            _wrappers.Add(eventType, new EventWrapper0());
        }

        public void RegisterEvent<T>(TEventType eventType)
        {
            Assert.That(!_wrappers.ContainsKey(eventType));
            _wrappers.Add(eventType, new EventWrapper1<T>());
        }

        public void RegisterEvent<T1, T2>(TEventType eventType)
        {
            Assert.That(!_wrappers.ContainsKey(eventType));
            _wrappers.Add(eventType, new EventWrapper2<T1, T2>());
        }

        public void RegisterEvent<T1, T2, T3>(TEventType eventType)
        {
            Assert.That(!_wrappers.ContainsKey(eventType));
            _wrappers.Add(eventType, new EventWrapper3<T1, T2, T3>());
        }

        public void RegisterEvent<T1, T2, T3, T4>(TEventType eventType)
        {
            Assert.That(!_wrappers.ContainsKey(eventType));
            _wrappers.Add(eventType, new EventWrapper4<T1, T2, T3, T4>());
        }

        ////////////////////////// Add Methods

        public void Subscribe(TEventType eventType, Action eventDelegate)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            _wrappers[eventType].Add(eventDelegate);
        }

        public void Subscribe<T>(TEventType eventType, Action<T> eventDelegate)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType +"'");
            _wrappers[eventType].Add(eventDelegate);
        }

        public void Subscribe<T1, T2>(TEventType eventType, Action<T1, T2> eventDelegate)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            _wrappers[eventType].Add(eventDelegate);
        }

        public void Subscribe<T1, T2, T3>(TEventType eventType, Action<T1, T2, T3> eventDelegate)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            _wrappers[eventType].Add(eventDelegate);
        }

        public void Subscribe<T1, T2, T3, T4>(TEventType eventType, Action<T1, T2, T3, T4> eventDelegate)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            _wrappers[eventType].Add(eventDelegate);
        }

        ////////////////////////// Remove Methods

        public void Unsubscribe(TEventType eventType, Action eventDelegate)
        {
            _wrappers[eventType].Remove(eventDelegate);
        }

        public void Unsubscribe<T>(TEventType eventType, Action<T> eventDelegate)
        {
            _wrappers[eventType].Remove(eventDelegate);
        }

        public void Unsubscribe<T1, T2>(TEventType eventType, Action<T1, T2> eventDelegate)
        {
            _wrappers[eventType].Remove(eventDelegate);
        }

        public void Unsubscribe<T1, T2, T3>(TEventType eventType, Action<T1, T2, T3> eventDelegate)
        {
            _wrappers[eventType].Remove(eventDelegate);
        }

        ////////////////////////// Invoke Methods

        public virtual void Invoke(TEventType eventType)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            Log.Debug("Invoking global event with type '{0}'", eventType.ToString());
            _wrappers[eventType].Invoke();
        }

        public virtual void Invoke<T>(TEventType eventType, T p1)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            Log.Debug("Invoking global event with type '{0}'", eventType.ToString());
            _wrappers[eventType].Invoke(p1);
        }

        public virtual void Invoke<T1, T2>(TEventType eventType, T1 p1, T2 p2)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            Log.Debug("Invoking global event with type '{0}'", eventType.ToString());
            _wrappers[eventType].Invoke(p1, p2);
        }

        public virtual void Invoke<T1, T2, T3>(TEventType eventType, T1 p1, T2 p2, T3 p3)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            Log.Debug("Invoking global event with type '{0}'", eventType.ToString());
            _wrappers[eventType].Invoke(p1, p2, p3);
        }

        public virtual void Invoke<T1, T2, T3, T4>(TEventType eventType, T1 p1, T2 p2, T3 p3, T4 p4)
        {
            Assert.That(_wrappers.ContainsKey(eventType), "Unregistered event with type '" + eventType + "'");
            Log.Debug("Invoking global event with type '{0}'", eventType.ToString());
            _wrappers[eventType].Invoke(p1, p2, p3, p4);
        }

        ////////////////////////// Event Wrapper

        // We could use Delegate instead of these classes (along with DynamicInvoke)
        // however this would not catch duplicate adds and failed removes (which
        // trigger asserts here)
        // DynamicInvoke is also much slower than this
        interface IEventWrapper
        {
            void Add(object handler);
            void Remove(object handler);
            void Invoke(params object[] args);
        }

        abstract class EventWrapper<T> : IEventWrapper
        {
            protected List<T> _handlers = new List<T>();

            public void Add(object handler)
            {
                Assert.That(!_handlers.Contains((T)handler));
                _handlers.Add((T)handler);
            }

            public void Remove(object handler)
            {
                bool removed = _handlers.Remove((T)handler);
                Assert.That(removed);
            }

            public abstract void Invoke(params object[] args);
        }

        class EventWrapper0 : EventWrapper<Action>
        {
            public override void Invoke(params object[] args)
            {
                Assert.That(args.Length == 0);

                foreach (var handler in _handlers)
                {
                    handler();
                }
            }
        }

        class EventWrapper1<T> : EventWrapper<Action<T>>
        {
            public override void Invoke(params object[] args)
            {
                Assert.That(args.Length == 1);

                foreach (var handler in _handlers)
                {
                    handler((T)args[0]);
                }
            }
        }

        class EventWrapper2<T1, T2> : EventWrapper<Action<T1, T2>>
        {
            public override void Invoke(params object[] args)
            {
                Assert.That(args.Length == 2);

                foreach (var handler in _handlers)
                {
                    handler((T1) args[0], (T2) args[1]);
                }
            }
        }

        class EventWrapper3<T1, T2, T3> : EventWrapper<Action<T1, T2, T3>>
        {
            public override void Invoke(params object[] args)
            {
                Assert.That(args.Length == 3);

                foreach (var handler in _handlers)
                {
                    handler((T1)args[0], (T2)args[1], (T3)args[2]);
                }
            }
        }

        class EventWrapper4<T1, T2, T3, T4> : EventWrapper<Action<T1, T2, T3, T4>>
        {
            public override void Invoke(params object[] args)
            {
                Assert.That(args.Length == 4);

                foreach (var handler in _handlers)
                {
                    handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]);
                }
            }
        }
    }
}
