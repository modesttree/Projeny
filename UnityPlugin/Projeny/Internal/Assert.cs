using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Projeny.Internal
{
    public static class Assert
    {
        public static void That(bool condition)
        {
            if (!condition)
            {
                Throw("Assert hit!");
            }
        }

        public static void IsType<T>(object obj)
        {
            IsType<T>(obj, "");
        }

        public static void IsType<T>(object obj, string message)
        {
            if (!(obj is T))
            {
                Throw("Assert Hit! Wrong type found. Expected '"+ typeof(T).Name + "' but found '" + obj.GetType().Name + "'. " + message);
            }
        }

        // Use AssertEquals to get better error output (with values)
        public static void IsEqual(object left, object right)
        {
            IsEqual(left, right, "");
        }

        public static void Throws(Action action)
        {
            Throws<Exception>(action);
        }

        public static void Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            Throw(string.Format("Expected to receive exception of type '{0}' but nothing was thrown", typeof(TException).Name));
        }

        // Use AssertEquals to get better error output (with values)
        public static void IsEqual(object left, object right, Func<string> messageGenerator)
        {
            if (!object.Equals(left, right))
            {
                left = left ?? "<NULL>";
                right = right ?? "<NULL>";
                Throw("Assert Hit! Expected '" + right.ToString() + "' but found '" + left.ToString() + "'. " + messageGenerator());
            }
        }

        // Use AssertEquals to get better error output (with values)
        public static void IsEqual(object left, object right, string message)
        {
            if (!object.Equals(left, right))
            {
                left = left ?? "<NULL>";
                right = right ?? "<NULL>";
                Throw("Assert Hit! Expected '" + right.ToString() + "' but found '" + left.ToString() + "'. " + message);
            }
        }

        // Use Assert.IsNotEqual to get better error output (with values)
        public static void IsNotEqual(object left, object right)
        {
            IsNotEqual(left, right, "");
        }

        // Use Assert.IsNotEqual to get better error output (with values)
        public static void IsNotEqual(object left, object right, Func<string> messageGenerator)
        {
            if(object.Equals(left, right))
            {
                left = left ?? "<NULL>";
                right = right ?? "<NULL>";
                Throw("Assert Hit! Expected '" + right.ToString() + "' to differ from '" + left.ToString() + "'. " + messageGenerator());
            }
        }

        public static void IsNull(object val)
        {
            if (val != null)
            {
                Throw("Assert Hit! Expected null pointer but instead found '" + val.ToString() + "'");
            }
        }

        public static void IsNotNull(object val)
        {
            if (val == null)
            {
                Throw("Assert Hit! Found null pointer when value was expected");
            }
        }

        public static void IsNotNull(object val, string message)
        {
            if (val == null)
            {
                Throw("Assert Hit! Found null pointer when value was expected. " + message);
            }
        }

        public static void IsNull(object val, string message, params object[] parameters)
        {
            if (val != null)
            {
                Throw("Assert Hit! Expected null pointer but instead found '" + val.ToString() + "': " + FormatString(message, parameters));
            }
        }

        public static void IsNotNull(object val, string message, params object[] parameters)
        {
            if (val == null)
            {
                Throw("Assert Hit! Found null pointer when value was expected. " + FormatString(message, parameters));
            }
        }

        // Use Assert.IsNotEqual to get better error output (with values)
        public static void IsNotEqual(object left, object right, string message)
        {
            if (object.Equals(left, right))
            {
                left = left ?? "<NULL>";
                right = right ?? "<NULL>";
                Throw("Assert Hit! Expected '" + right.ToString() + "' to differ from '" + left.ToString() + "'. " + message);
            }
        }

        // Pass a function instead of a string for cases that involve a lot of processing to generate a string
        // This way the processing only occurs when the assert fails
        public static void That(bool condition, Func<string> messageGenerator)
        {
            if (!condition)
            {
                Throw("Assert hit! " + messageGenerator());
            }
        }

        public static void That(
            bool condition, string message, params object[] parameters)
        {
            if (!condition)
            {
                Throw("Assert hit! " + FormatString(message, parameters));
            }
        }

        public static void Throw()
        {
            throw new Exception("Assert Hit!");
        }

        public static void Throw(string message)
        {
            throw new Exception(message);
        }

        public static void Throw(string message, params object[] parameters)
        {
            throw new Exception(
                FormatString(message, parameters));
        }

        static string FormatString(string format, params object[] parameters)
        {
            // doin this funky loop to ensure nulls are replaced with "NULL"
            // and that the original parameters array will not be modified
            if (parameters != null && parameters.Length > 0)
            {
                object[] paramToUse = parameters;

                foreach (object cur in parameters)
                {
                    if (cur == null)
                    {
                        paramToUse = new object[parameters.Length];

                        for (int i = 0; i < parameters.Length; ++i)
                        {
                            paramToUse[i] = parameters[i] ?? "NULL";
                        }

                        break;
                    }
                }

                format = string.Format(format, paramToUse);
            }

            return format;
        }
    }
}
