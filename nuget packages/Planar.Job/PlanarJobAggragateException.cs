using System;

namespace Planar
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly

    [Serializable]
    public class PlanarJobAggragateException : Exception
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    {
        public PlanarJobAggragateException(string message)
            : base(message)
        {
        }

        public PlanarJobAggragateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}