using System;
using System.Collections.Generic;
using System.Text;

namespace Projeny.Internal
{
    public static class ExceptionExtensions
    {
        public static string GetFullMessage(this Exception exception)
        {
            return exception.GetFullMessage(true);
        }

        public static string GetFullMessage(this Exception exception, bool includeExceptionClassNames)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                if (includeExceptionClassNames)
                {
                    stringBuilder.AppendLine("{0}: ".Fmt(exception.GetType().Name));
                }

                stringBuilder.AppendLine(exception.Message);
                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }
    }
}
