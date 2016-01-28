using System;
using System.Collections.Generic;

namespace ModestTree.Util
{
    public class Tuple<T1, T2>
    {
        public readonly T1 First;
        public readonly T2 Second;

        public Tuple()
        {
            First = default(T1);
            Second = default(T2);
        }

        public Tuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }

        public override bool Equals(Object obj)
        {
            var that = obj as Tuple<T1, T2>;

            if (that == null)
            {
                return false;
            }

            return Equals(that);
        }

        public bool Equals(Tuple<T1, T2> that)
        {
            if (that == null)
            {
                return false;
            }

            return object.Equals(First, that.First) && object.Equals(Second, that.Second);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 29 + (First == null ? 0 : First.GetHashCode());
                hash = hash * 29 + (Second == null ? 0 : Second.GetHashCode());
                return hash;
            }
        }
    }

    public class Tuple<T1, T2, T3>
    {
        public readonly T1 First;
        public readonly T2 Second;
        public readonly T3 Third;

        public Tuple()
        {
            First = default(T1);
            Second = default(T2);
            Third = default(T3);
        }

        public Tuple(T1 first, T2 second, T3 third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        public override bool Equals(Object obj)
        {
            var that = obj as Tuple<T1, T2, T3>;

            if (that == null)
            {
                return false;
            }

            return Equals(that);
        }

        public bool Equals(Tuple<T1, T2, T3> that)
        {
            if (that == null)
            {
                return false;
            }

            return object.Equals(First, that.First) && object.Equals(Second, that.Second) && object.Equals(Third, that.Third);
        }

        public override int GetHashCode()
        {
            return 17 * First.GetHashCode() + 31 * Second.GetHashCode() + 47 * Third.GetHashCode();
        }
    }

    public static class Tuple
    {
        public static Tuple<T1, T2> New<T1, T2>(T1 first, T2 second)
        {
            return new Tuple<T1, T2>(first, second);
        }

        public static Tuple<T1, T2, T3> New<T1, T2, T3>(T1 first, T2 second, T3 third)
        {
            return new Tuple<T1, T2, T3>(first, second, third);
        }
    }
}
