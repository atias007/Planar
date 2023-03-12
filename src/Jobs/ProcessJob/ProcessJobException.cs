using System.Runtime.Serialization;

namespace ProcessJob
{
    [Serializable]
    public class ProcessJobException : Exception
    {
        public ProcessJobException(string message) : base(message)
        {
        }

        protected ProcessJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}