using System.Runtime.Serialization;

namespace SqlJob
{
    [Serializable]
    public class SqlJobException : Exception
    {
        public SqlJobException(string message) : base(message)
        {
        }

        public SqlJobException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SqlJobException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}