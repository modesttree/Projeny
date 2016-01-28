using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;
using ModestTree.Util;
using System.IO;

namespace ModestTree.Util
{
    public class CoRoutineTimeoutException : Exception
    {
    }

    public class CoRoutineException : Exception
    {
        readonly List<Type> _objTrace;

        public CoRoutineException(List<Type> objTrace, Exception innerException)
            : base(CreateMessage(objTrace), innerException)
        {
            _objTrace = objTrace;
        }

        static string CreateMessage(List<Type> objTrace)
        {
            var result = new StringBuilder();

            foreach (var objType in objTrace)
            {
                if (result.Length != 0)
                {
                    result.Append(" -> ");
                }

                result.Append(objType.Name());
            }

            result.AppendLine();
            return "Coroutine Object Trace: " + result.ToString();
        }

        public List<Type> ObjectTrace
        {
            get
            {
                return _objTrace;
            }
        }
    }

    // Wrapper class for IEnumerator coroutines to allow calling nested coroutines easily
    public class CoRoutine
    {
        readonly Stack<IEnumerator> _processStack;
        object _returnValue;

        public CoRoutine(IEnumerator enumerator)
        {
            _processStack = new Stack<IEnumerator>();
            _processStack.Push(enumerator);
        }

        public object ReturnValue
        {
            get
            {
                Assert.That(_processStack.IsEmpty());
                return _returnValue;
            }
        }

        public bool IsDone
        {
            get
            {
                return _processStack.IsEmpty();
            }
        }

        public bool Pump()
        {
            Assert.That(!_processStack.IsEmpty());
            Assert.IsNull(_returnValue);

            var finished = new List<IEnumerator>();
            var topWorker = _processStack.Peek();

            bool isFinished;

            try
            {
                isFinished = !topWorker.MoveNext();
            }
            catch (CoRoutineException e)
            {
                var objectTrace = GenerateObjectTrace(finished.Concat(_processStack));

                if (objectTrace.IsEmpty())
                {
                    throw e;
                }

                throw new CoRoutineException(objectTrace.Concat(e.ObjectTrace).ToList(), e.InnerException);
            }
            catch (Exception e)
            {
                var objectTrace = GenerateObjectTrace(finished.Concat(_processStack));

                if (objectTrace.IsEmpty())
                {
                    throw e;
                }

                throw new CoRoutineException(objectTrace, e);
            }

            if (isFinished)
            {
                finished.Add(_processStack.Pop());
            }

            if (topWorker.Current != null && topWorker.Current.GetType().DerivesFrom<IEnumerator>())
            {
                _processStack.Push((IEnumerator)topWorker.Current);
            }

            if (_processStack.IsEmpty())
            {
                _returnValue = topWorker.Current;
            }

            return !_processStack.IsEmpty();
        }

        static List<Type> GenerateObjectTrace(IEnumerable<IEnumerator> enumerators)
        {
            var objTrace = new List<Type>();

            foreach (var enumerator in enumerators)
            {
                var field = enumerator.GetType().GetField("<>4__this", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                if (field == null)
                {
                    // Mono seems to use a different name
                    field = enumerator.GetType().GetField("<>f__this", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                    if (field == null)
                    {
                        continue;
                    }
                }

                var obj = field.GetValue(enumerator);

                if (obj == null)
                {
                    continue;
                }

                var objType = obj.GetType();

                if (objTrace.IsEmpty() || objType != objTrace.Last())
                {
                    objTrace.Add(objType);
                }
            }

            objTrace.Reverse();
            return objTrace;
        }

        public static IEnumerator<T> Wrap<T>(IEnumerator runner)
        {
            var coroutine = new CoRoutine(runner);

            while (coroutine.Pump())
            {
                yield return default(T);
            }

            if (coroutine.ReturnValue != null)
            {
                Assert.That(coroutine.ReturnValue.GetType().DerivesFromOrEqual<T>(),
                    "Unexpected type returned from coroutine!  Expected '{0}' and found '{1}'", typeof(T).Name(), coroutine.ReturnValue.GetType().Name());
            }

            yield return (T)coroutine.ReturnValue;
        }

        public static void SyncWait(IEnumerator runner)
        {
            var coroutine = new CoRoutine(runner);

            while (coroutine.Pump())
            {
            }
        }

        public static void SyncWaitWithTimeout(IEnumerator runner, float timeout)
        {
            var startTime = DateTime.UtcNow;
            var coroutine = new CoRoutine(runner);

            while (coroutine.Pump())
            {
                if ((DateTime.UtcNow - startTime).TotalSeconds > timeout)
                {
                    throw new CoRoutineTimeoutException();
                }
            }
        }

        public static T SyncWaitGet<T>(IEnumerator<T> runner)
        {
            var coroutine = new CoRoutine(runner);

            while (coroutine.Pump())
            {
            }

            return (T)coroutine.ReturnValue;
        }

        public static T SyncWaitGet<T>(IEnumerator runner)
        {
            var coroutine = new CoRoutine(runner);

            while (coroutine.Pump())
            {
            }

            return (T)coroutine.ReturnValue;
        }

        public static IEnumerator MakeParallelGroup(IEnumerable<IEnumerator> runners)
        {
            var runnerList = runners.Select(x => new CoRoutine(x)).ToList();

            while (runnerList.Any())
            {
                foreach (var runner in runnerList)
                {
                    runner.Pump();
                }

                foreach (var runner in runnerList.Where(x => x.IsDone).ToList())
                {
                    runnerList.Remove(runner);
                }

                yield return null;
            }
        }
    }
}
