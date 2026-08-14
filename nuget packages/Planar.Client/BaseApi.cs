using System;
using System.Net.Http;

namespace Planar.Client
{
    internal class BaseApi
    {
        protected readonly RestProxy _proxy;

        protected readonly static HttpMethod HttpPatchMethod = new HttpMethod("Patch");

        public BaseApi(RestProxy proxy)
        {
            _proxy = proxy;
        }

#if NETSTANDARD2_0

        protected static void ValidateMandatory(string value, string name)
#else
        protected static void ValidateMandatory(string? value, string name)
#endif
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"'{name}' cannot be null or whitespace", name);
            }
        }

        protected static void ValidateMandatory(long value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentException($"'{name}' must be greater then 0", name);
            }
        }

        protected static void ValidateMandatory(TimeSpan value, string name)
        {
            if (value == TimeSpan.Zero)
            {
                throw new ArgumentException($"'{name}' cannot be zero", name);
            }
        }

        protected static void ValidateMandatory(DateTime? date, string name)
        {
            if (date == null || date == DateTime.MinValue)
            {
                throw new ArgumentException($"'{name}' cannot be null or default date time value", name);
            }
        }

        protected static void ValidateMandatory<T>(T value, string name) where T : class, new()
        {
            if (value == null)
            {
                throw new ArgumentException($"'{name}' cannot be null", name);
            }
        }
    }
}