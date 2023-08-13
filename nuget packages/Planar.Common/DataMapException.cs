using System;

namespace Planar
{
#pragma warning disable S3925 // "ISerializable" should be implemented correctly

    public class DataMapException : Exception
#pragma warning restore S3925 // "ISerializable" should be implemented correctly
    {
        public DataMapException(string message) : base(message)
        {
        }
    }
}