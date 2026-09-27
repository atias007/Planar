using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Planar.Client
{
    internal class RestRequest
    {
        private const string Json = "application/json";
        private const string Yaml = "application/yaml";

        public RestRequest(string resource, HttpMethod method)
        {
            Resource = resource;
            Method = method;
        }

        public RestRequest(string resource, HttpMethod method, string contentType)
        {
            Resource = resource;
            Method = method;
            ContentType = contentType;
        }

        private readonly Dictionary<string, object> _queryString = new Dictionary<string, object>();
        private readonly Dictionary<string, object> _urlSegments = new Dictionary<string, object>();

        public string Resource { get; private set; }
        public HttpMethod Method { get; private set; }
        public string ContentType { get; set; } = Json;

#if NETSTANDARD2_0
        public object Body { get; private set; }
#else
        public object? Body { get; private set; }

#endif

#if NETSTANDARD2_0
        public string StringBody { get; private set; }
#else
        public string? StringBody { get; private set; }

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

        public RestRequest AddJsonStringBody(string body)
        {
            StringBody = body;
            ContentType = Json;
            return this;
        }

        public RestRequest AddYamlStringBody(string body)
        {
            StringBody = body;
            ContentType = Yaml;
            return this;
        }

        public RestRequest SetTimeoutSeconds(int seconds)
        {
            return SetTimeout(TimeSpan.FromSeconds(seconds));
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
            var url = GetUrl();
            var request = new HttpRequestMessage(Method, url);
            string body;

            if (!string.IsNullOrWhiteSpace(StringBody))
            {
                body = StringBody;
            }
            else if (Body != null)
            {
                body = CoreSerializer.Serialize(Body) ?? string.Empty;
            }
            else
            {
                return request;
            }

            var content = new StringContent(body, Encoding.UTF8, ContentType);
            request.Content = content;
            if (request.Content.Headers.ContentType != null)
            {
                request.Content.Headers.ContentType.MediaType = ContentType;
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