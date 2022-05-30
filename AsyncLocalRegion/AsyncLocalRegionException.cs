using System;
using System.Runtime.Serialization;

namespace AsyncLocalRegion
{
    [Serializable]
    public class AsyncLocalRegionException : ApplicationException
    {
        public AsyncLocalRegionException()
        {
        }

        public AsyncLocalRegionException(string message) : base(message)
        {
        }

        public AsyncLocalRegionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected AsyncLocalRegionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}