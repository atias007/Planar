using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Planar.Client
{
    internal class RestRequest
    {
        public RestRequest(string resource, HttpMethod method)
        {
            Resource = resource;
            Method = method;
        }

        private readonly Dictionary<string, object> _queryString = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _urlSegments = new Dictionary<string, object>();

        public string Resource { get; private set; }
        public HttpMethod Method { get; private set; }

#if NETSTANDARD2_0
        public object Body { get; private set; }
#else
        public object? Body { get; private set; }

#endif

#if NETSTANDARD2_0
        public TimeSpan Timeout { get; private set; }
#else
        public TimeSpan? Timeout { get; private set; }

#endif

        public RestRequest AddBody(object body)
        {
            Body = body;
            return this;
        }

        public RestRequest SetTimeoutSeconds(int secondes)
        {
            return SetTimeout(TimeSpan.FromSeconds(secondes));
        }

        public RestRequest SetTimeout(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

#if NETSTANDARD2_0

        public RestRequest AddQueryParameter(string name, object value)

#else
        public RestRequest AddQueryParameter(string name, object? value)

#endif
        {
            if (string.IsNullOrWhiteSpace(name)) { return this; }
            if (value == null) { return this; }

            var strValue = value.ToString();
            if (string.IsNullOrWhiteSpace(strValue)) { return this; }

            if (_queryString.ContainsKey(name))
            {
                _queryString[name] = value;
            }
            else
            {
                _queryString.Add(name, value);
            }

            return this;
        }

#if NETSTANDARD2_0

        public RestRequest AddSegmentParameter(string name, object value)

#else
        public RestRequest AddSegmentParameter(string name, object? value)

#endif
        {
            if (string.IsNullOrWhiteSpace(name)) { return this; }
            if (value == null) { return this; }

            var strValue = value.ToString();
            if (string.IsNullOrWhiteSpace(strValue)) { return this; }

            if (_urlSegments.ContainsKey(name))
            {
                _urlSegments[name] = value;
            }
            else
            {
                _urlSegments.Add(name, value);
            }

            return this;
        }

        public HttpRequestMessage GetRequest()
        {
            const string contentType = "application/json";
            var url = GetUrl();
            var request = new HttpRequestMessage(Method, url);
            if (Body != null)
            {
                var jsonBody = CoreSerializer.Serialize(Body) ?? string.Empty;
                var content = new StringContent(jsonBody, Encoding.UTF8, contentType);
                request.Content = content;
                if (request.Content.Headers.ContentType != null)
                {
                    request.Content.Headers.ContentType.MediaType = contentType;
                }
            }

            return request;
        }

        public string GetUrl()
        {
            var url = Resource;
            foreach (var segment in _urlSegments)
            {
                url = url.Replace($"{{{segment.Key}}}", segment.Value.ToString());
            }

            if (_queryString.Count > 0)
            {
                var query = new StringBuilder();
                foreach (var item in _queryString)
                {
                    if (query.Length > 0) { query.Append("&"); }
                    query.Append($"{item.Key}={item.Value}");
                }
                url += "?" + query;
            }

            return url;
        }
    }
}