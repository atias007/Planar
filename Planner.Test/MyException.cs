using System;
using System.Runtime.Serialization;

namespace Planner.Test
{
    [Serializable]
    public class MyException : Exception
    {
        public MyException(string message, Exception innerException)
            : base(message, innerException)
        {
            PlannerException = innerException;
        }

        public MyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            PlannerException = (Exception)info.GetValue(nameof(PlannerException), typeof(Exception));
        }

        public Exception PlannerException { get; }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(PlannerException), PlannerException);
        }
    }
}