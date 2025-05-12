using Planar.Client.Entities;
using System;
using System.Globalization;
using System.Reflection;

namespace Planar.Client
{
    internal static class RestRequestExtensions
    {
        public static RestRequest AddQueryPagingParameter(this RestRequest request, int pageSize)
        {
            if (pageSize > 0)
            {
                request.AddQueryParameter(nameof(IPaging.PageSize), pageSize);
            }

            return request;
        }

        public static RestRequest AddQueryPagingParameter(this RestRequest request, int pageSize, int pageNumber)
        {
            request.AddQueryPagingParameter(pageSize);

            if (pageNumber > 0)
            {
                request.AddQueryParameter(nameof(IPaging.PageNumber), pageNumber);
            }

            return request;
        }

        public static RestRequest AddQueryPagingParameter(this RestRequest request, IPaging pagingRequest)
        {
            if (pagingRequest == null) { return request; }
            pagingRequest.SetPagingDefaults();
            request.AddQueryPagingParameter(pagingRequest.PageSize.GetValueOrDefault(), pagingRequest.PageNumber.GetValueOrDefault());
            return request;
        }

        public static RestRequest AddQueryDateScope(this RestRequest request, IDateScope dateScopeRequest)
        {
            if (dateScopeRequest.FromDate.HasValue && dateScopeRequest.FromDate > DateTime.MinValue)
            {
                request.AddQueryParameter(nameof(IDateScope.FromDate), dateScopeRequest.FromDate.Value.ToString("u"));
            }

            if (dateScopeRequest.ToDate.HasValue && dateScopeRequest.ToDate > DateTime.MinValue)
            {
                request.AddQueryParameter(nameof(IDateScope.ToDate), dateScopeRequest.ToDate.Value.ToString("u"));
            }

            return request;
        }

        public static RestRequest AddEntityToQueryParameter<T>(this RestRequest request, T parameter)
        where T : class
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var item in props)
            {
                var name = item.Name;

                var value = item.GetValue(parameter, null);
                if (value == null) { continue; }

                var type = value.GetType();
                var @default = type.IsValueType ? Activator.CreateInstance(type) : null;
                if (value.Equals(@default)) { continue; }
                var stringValue = GetStringValueForQueryStringParameter(value);
                request.AddQueryParameter(name, stringValue);
            }

            return request;
        }

#if NETSTANDARD2_0

        private static string GetStringValueForQueryStringParameter(object value)
#else
        private static string? GetStringValueForQueryStringParameter(object value)
#endif
        {
            const string DateFormat = "s";

            if (value is DateTime)
            {
                var dateValue = (DateTime)value;
                return dateValue.ToString(DateFormat);
            }

            if (value is DateTimeOffset)
            {
                var dateValue = (DateTimeOffset)value;
                return dateValue.ToString(DateFormat);
            }

            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }
    }
}