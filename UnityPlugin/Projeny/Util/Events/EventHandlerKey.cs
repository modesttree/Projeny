using System;
using System.Reflection;

namespace Projeny.Internal
{
    public class EventHandlerKey : IEquatable<EventHandlerKey>
    {
        MethodInfo _methodInfo;
        object _target;
        object _sender;
        int _hashcode;

        public EventHandlerKey(object target, MethodInfo methodInfo, object sender)
        {
            Assert.IsNotNull(methodInfo);
            Assert.IsNotNull(target);

            _target = target;
            _sender = sender;
            _methodInfo = methodInfo;

            _hashcode = target.GetHashCode() ^ methodInfo.GetHashCode();

            if (sender != null)
            {
                _hashcode ^= _sender.GetHashCode();
            }
        }

        public EventHandlerKey(Delegate del, object sender)
            : this(del.Target, del.Method, sender)
        {
        }

        public MethodInfo Method
        {
            get
            {
                return _methodInfo;
            }
        }

        public object Target
        {
            get
            {
                return _target;
            }
        }

        public object Sender
        {
            get
            {
                return _sender;
            }
        }

        public override int GetHashCode()
        {
            return _hashcode;
        }

        public override bool Equals(object other)
        {
            if (other is EventHandlerKey)
            {
                EventHandlerKey otherKey = (EventHandlerKey)other;
                return otherKey == this;
            }
            else
            {
                return false;
            }
        }

        public bool Equals(EventHandlerKey that)
        {
            if ((_sender == null && that._sender != null)
                || (that._sender == null && _sender != null))
            {
                return false;
            }

            if (_sender != null)
            {
                Assert.That(that._sender != null);

                if (!_sender.Equals(that._sender))
                {
                    return false;
                }
            }

            return _target.Equals(that._target) && _methodInfo.Equals(that._methodInfo);
        }

        public override string ToString()
        {
            return _methodInfo.Name;
        }

        public static bool operator ==(EventHandlerKey left, EventHandlerKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EventHandlerKey left, EventHandlerKey right)
        {
            return !left.Equals(right);
        }
    }
}
