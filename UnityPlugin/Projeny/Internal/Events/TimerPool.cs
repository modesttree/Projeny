#if UNITY3D
using Zenject;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Projeny.Internal
{
    // Responsibilities:
    // - Starts an async timer of a set duration, and adds it to a collection.  Once the timer completes,
    //   the callback that was supplied when the timer started is called, and the timer is removed
    //   from the collection
    public class TimerPool : ITickable
    {
        List<Timer> _timers = new List<Timer>();

        [Inject]
        public TimerPool()
        {
        }

        public void Tick()
        {
            Timer[] activeThisFrame = _timers.ToArray();
            foreach (Timer cur in activeThisFrame)
            {
                if (_timers.Contains(cur))
                {
                    cur.Update();
                }
            }
        }

        public Timer Start(float delay = 1f, bool repeat = false, Action<Timer> callback = null)
        {
            Timer timer = new Timer(this);
            timer.Delay = delay;
            timer.Repeat = repeat;

            if (callback != null)
            {
                timer.Ticked += callback;
            }

            timer.Resume();
            return timer;
        }

        public class Timer
        {
            public event Action<Timer> Ticked = delegate { };
            public float Delay = 1f;
            public bool Repeat = false;
            public bool UsePreciseRepeats = true;

            readonly TimerPool _pool;
            float _elapsedTime;

            public Timer(TimerPool pool)
            {
                Assert.IsNotNull(pool);
                _pool = pool;
            }

            public void Start()
            {
                Reset();
                Resume();
            }

            public void Stop()
            {
                _pool._timers.Remove(this);
            }

            public void Resume()
            {
                if (!_pool._timers.Contains(this))
                {
                    // adding first so timers resumed during a Tick()
                    // don't get updated until the next frame
                    _pool._timers.Add(this);
                }
            }

            public void Reset()
            {
                _elapsedTime = 0f;
            }

            public void Update()
            {
                _elapsedTime += Time.deltaTime;

                if (_elapsedTime >= Delay)
                {
                    if (UsePreciseRepeats)
                    {
                        _elapsedTime -= Delay;
                    }
                    else
                    {
                        _elapsedTime = 0f;
                    }

                    if (!Repeat)
                    {
                        Stop();
                    }

                    Ticked(this);
                }
            }

            public class Factory : Factory<TimerPool>
            {
            }
        }

    }
}

#endif
