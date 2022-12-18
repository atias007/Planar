using Serilog.Core;
using Serilog.Events;
using System;
using System.Linq;

namespace Planar.Startup.Logging
{
    public class PlanarFilter : ILogEventFilter
    {
        private static readonly string[] filterPath = new[]
        {
            "/",
            "/service/healthcheck",
            "/cluster/healthcheck",
            "/favicon.ico"
        };

        private const string SwaggerRequestPath = "swagger";
        private const string ContentRequestPath = "/content";

        public bool IsEnabled(LogEvent logEvent)
        {
            if (logEvent.Properties.ContainsKey("RequestPath"))
            {
                var value = logEvent.Properties["RequestPath"] as ScalarValue;
                var stringValue = Convert.ToString(value?.Value);
                if (string.IsNullOrEmpty(stringValue)) { return true; }

                stringValue = stringValue.ToLower();
                if (filterPath.Contains(stringValue))
                {
                    return false;
                }

                if (stringValue.Contains(SwaggerRequestPath))
                {
                    return false;
                }

                if (stringValue.StartsWith(ContentRequestPath))
                {
                    return false;
                }
            }

            return true;
        }
    }
}