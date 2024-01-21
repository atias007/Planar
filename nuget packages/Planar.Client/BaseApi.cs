using System;

namespace Planar.Client
{
    internal class BaseApi
    {
        protected readonly RestProxy _proxy;

        public BaseApi(RestProxy proxy)
        {
            _proxy = proxy;
        }

        protected void ValidateMandatory(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"'{name}' cannot be null or whitespace", name);
            }
        }

        protected void ValidateMandatory(long value, string name)
        {
            if (value <= 0)
            {
                throw new ArgumentException($"'{name}' must be greater then 0", name);
            }
        }
    }
}