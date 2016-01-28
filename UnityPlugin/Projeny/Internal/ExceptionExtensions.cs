using System;
using System.Collections.Generic;
using System.Text;

namespace Projeny.Internal
{
    public static class ExceptionExtensions
    {
        public static string GetFullMessage(this Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(string.Format("{0}: {1}", exception.GetType().Name, exception.Message));
                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
