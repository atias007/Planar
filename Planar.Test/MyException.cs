using System;
using System.Runtime.Serialization;

namespace Planar.Test
{
    [Serializable]
    public class MyException : Exception
    {
        public MyException(string message, Exception innerException)
            : base(message, innerException)
        {
            PlanarException = innerException;
        }

        public MyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            PlanarException = (Exception)info.GetValue(nameof(PlanarException), typeof(Exception));
        }

        public Exception PlanarException { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(PlanarException), PlanarException);
        }
    }
}