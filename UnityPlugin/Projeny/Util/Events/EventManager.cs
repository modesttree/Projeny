using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ModestTree.Util;

#if UNITY3D
using UnityEngine;
#endif

namespace ModestTree.Util
{
    [Flags]
    public enum EventQueueMode
    {
        // Do not wait for Flush() - call the handler immediately
        // This is the default
        Synchronous,
        // Queue all events and trigger them all in the order received when Flush() is called
        All,
        // Call the handler with only most recently received event info when Flush() is called (max one per Flush())
        LatestOnly,
        // Call the handler with the earliest received event info when Flush() is called (max one per Flush())
        FirstOnly,
        // With these set, the event(s) will not be triggered if Flush()
        // is called in the same frame in which it was originally invoked
        AllNextFrame,
        LatestOnlyNextFrame,
        FirstOnlyNextFrame,
    }

    // Responsibilities:
    // - Queues event arguments from a remote source, then sends them to its local target
    //   when requested
    public class EventManager
    {
        Dictionary<EventHandlerKey, HandlerInfo> _handlers = new Dictionary<EventHandlerKey, HandlerInfo>();
        List<QueueEntry> _queue = new List<QueueEntry>();
        IFrameCounter _frameCounter;

        [Preserve]
        public EventManager(IFrameCounter frameCounter)
        {
            _frameCounter = frameCounter;
        }

        public int NumListeners
        {
            get
            {
                return _handlers.Count;
            }
        }

        void RemoveHandlerItemsFromQueue(HandlerInfo handlerInfo)
        {
            foreach (var entry in _queue.Where(x => x.HandlerInfo == handlerInfo).ToList())
            {
                _queue.Remove(entry);
            }
        }

        bool IsProfilingEnabled()
        {
            return false;
        }

        public void OnRemoteInvoked(EventHandlerKey key, object[] args)
        {
            Assert.That(_handlers.ContainsKey(key),
                "Received invoke for unrecognized event '{0}'", key);

            HandlerInfo handlerInfo;

            // If we can't find the handler than the local object that owns
            // the event handler is probably deleted so just ignore
            if (_handlers.TryGetValue(key, out handlerInfo))
            {
                OnRemoteInvoked(_handlers[key], args);
            }
        }

        public void OnRemoteInvoked(HandlerInfo handlerInfo, object[] args)
        {
            var entry = new QueueEntry
            {
                HandlerInfo = handlerInfo,
                InvocationArguments = args,
                FrameNumber = (_frameCounter == null ? 0 : _frameCounter.FrameCount),
            };

            if (handlerInfo.Mode == EventQueueMode.Synchronous)
            {
                TriggerEvent(entry);
            }
            else
            {
                _queue.Add(entry);
            }
        }

        void RemoveDuplicatesBasedOnQueueMode(HandlerInfo handlerInfo, List<QueueEntry> queue)
        {
            switch (handlerInfo.Mode)
            {
                case EventQueueMode.Synchronous:
                {
                    // Should not have been queued in the first place
                    Assert.IsEqual(queue.Where(x => x.HandlerInfo == handlerInfo).Count(), 0);
                    break;
                }
                case EventQueueMode.AllNextFrame:
                case EventQueueMode.All:
                {
                    // Do nothing, trigger all events
                    break;
                }
                case EventQueueMode.LatestOnlyNextFrame:
                case EventQueueMode.LatestOnly:
                {
                    // Remove all but the last one
                    foreach (var item in queue.Where(x => x.HandlerInfo == handlerInfo).Reverse().Skip(1).ToList())
                    {
                        queue.Remove(item);
                    }

                    break;
                }
                case EventQueueMode.FirstOnlyNextFrame:
                case EventQueueMode.FirstOnly:
                {
                    // Remove all but the first one
                    foreach (var item in queue.Where(x => x.HandlerInfo == handlerInfo).Skip(1).ToList())
                    {
                        queue.Remove(item);
                    }

                    break;
                }
                default:
                {
                    Assert.That(false);
                    break;
                }
            }
        }

        bool ShouldProcessThisFrame(QueueEntry entry)
        {
            // If queue mode is NextFrame then only process events that occurred on a previous frame
            if (entry.HandlerInfo.Mode == EventQueueMode.LatestOnlyNextFrame || entry.HandlerInfo.Mode == EventQueueMode.AllNextFrame)
            {
                Assert.IsNotNull(_frameCounter);
                return entry.FrameNumber < _frameCounter.FrameCount;
            }

            return true;
        }

        public void Flush()
        {
            if (_queue.IsEmpty())
            {
                return;
            }

            var entriesToProcess = _queue.Where(ShouldProcessThisFrame).ToList();

            _queue.RemoveAll(ShouldProcessThisFrame);

            foreach (var handlerInfo in _handlers.Values)
            {
                RemoveDuplicatesBasedOnQueueMode(handlerInfo, entriesToProcess);
            }

            foreach (var entry in entriesToProcess)
            {
                // Do not trigger the event if it was removed during another handler
                if (_handlers.ContainsKey(entry.HandlerInfo.Key))
                {
                    if (entry.HandlerInfo.IsOneOff)
                    {
                        Assert.That(entriesToProcess.Where(x => x.HandlerInfo == entry.HandlerInfo).IsLength(1));
                        _handlers.RemoveWithConfirm(entry.HandlerInfo.Key);
                    }

                    TriggerEvent(entry);
                }
            }
        }

        string GetDelegateMethodName(Delegate del)
        {
            return del.Target.GetType().Name + "." + del.Method.Name;
        }

        void TriggerEvent(QueueEntry entry)
        {
            Delegate localDel;

            // Use delegate directly.  This is faster than using reflection to create the delegate as above
            Assert.IsNotNull(entry.HandlerInfo.LocalDelegate);
            localDel = entry.HandlerInfo.LocalDelegate;

            if (IsProfilingEnabled())
            {
                using (ProfileBlock.Start(GetDelegateMethodName(localDel)))
                {
                    localDel.DynamicInvoke(entry.InvocationArguments);
                }
            }
            else
            {
                localDel.DynamicInvoke(entry.InvocationArguments);
            }
        }

        public void AssertIsEmpty()
        {
            if (!_handlers.IsEmpty())
            {
                var output = new StringBuilder();

                output.AppendFormat("Found {0} events still registered: ", _handlers.Count);
                output.AppendLine();

                foreach (var pair in _handlers)
                {
                    output.Append(pair.Key.ToString());
                    output.AppendLine();
                }

                Assert.That(false, output.ToString());
            }
        }

        // This method can be used to trigger anonymous delegates
        // Assumes that the given delegate is unique
        public void TriggerOneOffNextFrame(Action localDelegate)
        {
            TriggerOneOff(localDelegate, EventQueueMode.AllNextFrame);
        }

        // This method can be used to trigger anonymous delegates
        // Assumes that the given delegate is unique
        public void TriggerOneOffNextFlush(Action localDelegate)
        {
            TriggerOneOff(localDelegate, EventQueueMode.All);
        }

        // We could make this public but there's only two queue modes
        // that make sense so it's easier this way
        void TriggerOneOff(Action localDelegate, EventQueueMode mode)
        {
            var key = new EventHandlerKey(localDelegate, null);

            if (_handlers.ContainsKey(key))
            {
                // Already queued
                return;
            }

            _handlers.Add(key,
                new HandlerInfo()
                {
                    UsageCount = 1,
                    Key = key,
                    Mode = mode,
                    IsOneOff = true,
                    DelegateType = typeof(Action),
                    LocalDelegate = localDelegate,
                });

            OnRemoteInvoked(key, new object[0]);
        }

        // These methods can be used to trigger your own handlers manually (rather than via remote events)
        public void Trigger(Action localDelegate)
        {
            TriggerInternal(null, localDelegate, new object[0]);
        }

        public void Trigger(object sender, Action localDelegate)
        {
            TriggerInternal(sender, localDelegate, new object[0]);
        }

        public void Trigger<TParam1>(
            Action<TParam1> localDelegate, TParam1 param)
        {
            TriggerInternal(null, localDelegate, new object[] { param });
        }

        public void Trigger<TParam1>(
            object sender, Action<TParam1> localDelegate, TParam1 param)
        {
            TriggerInternal(sender, localDelegate, new object[] { param });
        }

        public void Trigger<TParam1, TParam2>(
            Action<TParam1, TParam2> localDelegate, TParam1 param1, TParam2 param2)
        {
            TriggerInternal(null, localDelegate, new object[] { param1, param2 });
        }

        public void Trigger<TParam1, TParam2>(
            object sender, Action<TParam1, TParam2> localDelegate, TParam1 param1, TParam2 param2)
        {
            TriggerInternal(sender, localDelegate, new object[] { param1, param2 });
        }

        public void Trigger<TParam1, TParam2, TParam3>(
            Action<TParam1, TParam2, TParam3> localDelegate, TParam1 param1, TParam2 param2, TParam3 param3)
        {
            TriggerInternal(null, localDelegate, new object[] { param1, param2, param3 });
        }

        public void Trigger<TParam1, TParam2, TParam3>(
            object sender, Action<TParam1, TParam2, TParam3> localDelegate, TParam1 param1, TParam2 param2, TParam3 param3)
        {
            TriggerInternal(sender, localDelegate, new object[] { param1, param2, param3 });
        }

        void TriggerInternal(object sender, Delegate localDelegate, object[] args)
        {
            var key = new EventHandlerKey(localDelegate, sender);
            OnRemoteInvoked(key, args);
        }

        // Remove custom methods
        public TDelegateType RemoveInternal<TDelegateType>(
            object sender, TDelegateType localDelegate) where TDelegateType : class
        {
            var key = new EventHandlerKey((Delegate)(object)localDelegate, sender);

            var handlerInfo = _handlers[key];

            // There is a minor bug here where sometimes the handler items won't be removed
            // from the queue when UsageCount > 1
            if (handlerInfo.UsageCount == 1)
            {
                _handlers.RemoveWithConfirm(key);

                RemoveHandlerItemsFromQueue(handlerInfo);
            }
            else
            {
                handlerInfo.UsageCount--;
            }

            return (TDelegateType)(object)handlerInfo.OurDelegate;
        }

        // Add custom methods
        public TDelegateType AddInternal<TDelegateType>(
            EventHandlerKey key, EventQueueMode mode, object sender,
            Delegate localDelegate, Delegate ourDelegate)
        {
            Assert.IsNotNull(localDelegate);

            HandlerInfo handler;

            if (_handlers.TryGetValue(key, out handler))
            {
                Assert.IsEqual(mode, handler.Mode, "Cannot use the same event handler method with multiple different EventQueueMode's");

                handler.UsageCount++;
                return (TDelegateType)(object)handler.OurDelegate;
            }

            _handlers.Add(key,
                new HandlerInfo()
                {
                    UsageCount = 1,
                    Key = key,
                    Mode = mode,
                    OurDelegate = ourDelegate,
                    DelegateType = typeof(TDelegateType),
                    LocalDelegate = localDelegate,
                });

            return (TDelegateType)(object)ourDelegate;
        }

        // Zero parameters
        public Action Add(Action localDelegate, EventQueueMode mode = EventQueueMode.Synchronous)
        {
            return Add(null, localDelegate, mode);
        }

        public Action Add(object sender, Action localDelegate, EventQueueMode mode = EventQueueMode.Synchronous)
        {
            var key = new EventHandlerKey(localDelegate, sender);
            Action ourDelegate = () => OnRemoteInvoked(key, new object[0]);
            return AddInternal<Action>(key, mode, sender, localDelegate, ourDelegate);
        }

        // Remove
        public Action Remove(Action localDelegate)
        {
            return RemoveInternal<Action>(null, localDelegate);
        }

        public Action Remove(object sender, Action localDelegate)
        {
            return RemoveInternal<Action>(sender, localDelegate);
        }

        // One parameter
        public Action<TParam1> Add<TParam1>(Action<TParam1> localDelegate)
        {
            return Add<TParam1>(null, localDelegate);
        }

        public Action<TParam1> Add<TParam1>(
            object sender, Action<TParam1> localDelegate)
        {
            return Add<TParam1>(sender, localDelegate, EventQueueMode.Synchronous);
        }

        public Action<TParam1> Add<TParam1>(
            Action<TParam1> localDelegate, EventQueueMode mode)
        {
            return Add<TParam1>(null, localDelegate, mode);
        }

        public Action<TParam1> Add<TParam1>(
            object sender, Action<TParam1> localDelegate, EventQueueMode mode)
        {
            var key = new EventHandlerKey(localDelegate, sender);
            Action<TParam1> ourDelegate = (TParam1 param1) => OnRemoteInvoked(key, new object[] { param1 });
            return AddInternal<Action<TParam1>>(key, mode, sender, localDelegate, ourDelegate);
        }

        // Remove
        public Action<TParam1> Remove<TParam1>(Action<TParam1> localDelegate)
        {
            return RemoveInternal<Action<TParam1>>(null, localDelegate);
        }

        public Action<TParam1> Remove<TParam1>(object sender, Action<TParam1> localDelegate)
        {
            return RemoveInternal<Action<TParam1>>(sender, localDelegate);
        }

        //// Two parameters
        public Action<TParam1, TParam2> Add<TParam1, TParam2>(
            Action<TParam1, TParam2> localDelegate, EventQueueMode mode)
        {
            return Add<TParam1, TParam2>(null, localDelegate, mode);
        }

        public Action<TParam1, TParam2> Add<TParam1, TParam2>(
            Action<TParam1, TParam2> localDelegate)
        {
            return Add<TParam1, TParam2>(null, localDelegate, EventQueueMode.Synchronous);
        }

        public Action<TParam1, TParam2> Add<TParam1, TParam2>(
            object sender, Action<TParam1, TParam2> localDelegate)
        {
            return Add<TParam1, TParam2>(sender, localDelegate, EventQueueMode.Synchronous);
        }

        public Action<TParam1, TParam2> Add<TParam1, TParam2>(
            object sender, Action<TParam1, TParam2> localDelegate, EventQueueMode mode)
        {
            var key = new EventHandlerKey(localDelegate, sender);
            Action<TParam1, TParam2> ourDelegate = (TParam1 param1, TParam2 param2) => OnRemoteInvoked(key, new object[] { param1, param2 });
            return AddInternal<Action<TParam1, TParam2>>(key, mode, sender, localDelegate, ourDelegate);
        }

        // Remove
        public Action<TParam1, TParam2> Remove<TParam1, TParam2>(
            Action<TParam1, TParam2> localDelegate)
        {
            return RemoveInternal<Action<TParam1, TParam2>>(null, localDelegate);
        }

        public Action<TParam1, TParam2> Remove<TParam1, TParam2>(
            object sender, Action<TParam1, TParam2> localDelegate)
        {
            return RemoveInternal<Action<TParam1, TParam2>>(sender, localDelegate);
        }

        // Three parameters
        public Action<TParam1, TParam2, TParam3> Add<TParam1, TParam2, TParam3>(
            Action<TParam1, TParam2, TParam3> localDelegate)
        {
            return Add<TParam1, TParam2, TParam3>(null, localDelegate);
        }

        public Action<TParam1, TParam2, TParam3> Add<TParam1, TParam2, TParam3>(
            object sender, Action<TParam1, TParam2, TParam3> localDelegate)
        {
            return Add<TParam1, TParam2, TParam3>(sender, localDelegate, EventQueueMode.Synchronous);
        }

        public Action<TParam1, TParam2, TParam3> Add<TParam1, TParam2, TParam3>(
            Action<TParam1, TParam2, TParam3> localDelegate, EventQueueMode mode)
        {
            return Add<TParam1, TParam2, TParam3>(null, localDelegate, mode);
        }

        public Action<TParam1, TParam2, TParam3> Add<TParam1, TParam2, TParam3>(
            object sender, Action<TParam1, TParam2, TParam3> localDelegate, EventQueueMode mode)
        {
            var key = new EventHandlerKey(localDelegate, sender);
            Action<TParam1, TParam2, TParam3> ourDelegate = (TParam1 param1, TParam2 param2, TParam3 param3) => OnRemoteInvoked(key, new object[] { param1, param2, param3 });
            return AddInternal<Action<TParam1, TParam2, TParam3>>(key, mode, sender, localDelegate, ourDelegate);
        }

        // Remove
        public Action<TParam1, TParam2, TParam3> Remove<TParam1, TParam2, TParam3>(
            Action<TParam1, TParam2, TParam3> localDelegate)
        {
            return Remove<TParam1, TParam2, TParam3>(null, localDelegate);
        }

        public Action<TParam1, TParam2, TParam3> Remove<TParam1, TParam2, TParam3>(
            object sender, Action<TParam1, TParam2, TParam3> localDelegate)
        {
            return RemoveInternal<Action<TParam1, TParam2, TParam3>>(sender, localDelegate);
        }

        // Four parameters
        public Action<TParam1, TParam2, TParam3, TParam4> Add<TParam1, TParam2, TParam3, TParam4>(
            Action<TParam1, TParam2, TParam3, TParam4> localDelegate)
        {
            return Add<TParam1, TParam2, TParam3, TParam4>(null, localDelegate);
        }

        public Action<TParam1, TParam2, TParam3, TParam4> Add<TParam1, TParam2, TParam3, TParam4>(
            object sender, Action<TParam1, TParam2, TParam3, TParam4> localDelegate)
        {
            return Add<TParam1, TParam2, TParam3, TParam4>(sender, localDelegate, EventQueueMode.Synchronous);
        }

        public Action<TParam1, TParam2, TParam3, TParam4> Add<TParam1, TParam2, TParam3, TParam4>(
            Action<TParam1, TParam2, TParam3, TParam4> localDelegate, EventQueueMode mode)
        {
            return Add<TParam1, TParam2, TParam3, TParam4>(null, localDelegate, mode);
        }

        public Action<TParam1, TParam2, TParam3, TParam4> Add<TParam1, TParam2, TParam3, TParam4>(
            object sender, Action<TParam1, TParam2, TParam3, TParam4> localDelegate, EventQueueMode mode)
        {
            var key = new EventHandlerKey(localDelegate, sender);
            Action<TParam1, TParam2, TParam3, TParam4> ourDelegate = (TParam1 param1, TParam2 param2, TParam3 param3, TParam4 param4) => OnRemoteInvoked(key, new object[] { param1, param2, param3, param4 });
            return AddInternal<Action<TParam1, TParam2, TParam3, TParam4>>(key, mode, sender, localDelegate, ourDelegate);
        }

        // Remove
        public Action<TParam1, TParam2, TParam3, TParam4> Remove<TParam1, TParam2, TParam3, TParam4>(
            Action<TParam1, TParam2, TParam3, TParam4> localDelegate)
        {
            return Remove<TParam1, TParam2, TParam3, TParam4>(null, localDelegate);
        }

        public Action<TParam1, TParam2, TParam3, TParam4> Remove<TParam1, TParam2, TParam3, TParam4>(
            object sender, Action<TParam1, TParam2, TParam3, TParam4> localDelegate)
        {
            return RemoveInternal<Action<TParam1, TParam2, TParam3, TParam4>>(sender, localDelegate);
        }

        ///////////////////////////////////////////

        class QueueEntry
        {
            public HandlerInfo HandlerInfo;
            public object[] InvocationArguments;
            public int FrameNumber;
        }

        public class HandlerInfo
        {
            public EventHandlerKey Key;
            public bool IsOneOff;
            public EventQueueMode Mode;
            public Delegate OurDelegate;
            public bool IsRemoved;
            public Type DelegateType;
            public Delegate LocalDelegate;
            public int UsageCount;
        }
    }
}
