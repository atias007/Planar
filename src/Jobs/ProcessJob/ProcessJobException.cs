using Planar.Common.Exceptions;
using System.Runtime.Serialization;

namespace ProcessJob
{
    [Serializable]
    public class ProcessJobException : PlanarException
    {
        public ProcessJobException(string message) : base(message)
        {
        }

        protected ProcessJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}