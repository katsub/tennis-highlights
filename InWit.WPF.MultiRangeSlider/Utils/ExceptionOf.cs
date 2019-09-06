using System;

namespace InWit.Core.Utils
{
    public class ExceptionOf<T> : Exception
    {
        public ExceptionOf()
        {
        }

        public ExceptionOf(string message)
            : base(message)
        {

        }

        public ExceptionOf(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
