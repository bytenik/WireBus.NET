using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace WireBus
{
    [Serializable]
    public class InvalidEnvelopeException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public InvalidEnvelopeException()
        {
        }

        public InvalidEnvelopeException(string message) : base(message)
        {
        }

        public InvalidEnvelopeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidEnvelopeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
