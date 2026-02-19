using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace Planar.Startup.Logging;

public class PlanarFilter : ILogEventFilter
{
    private static readonly HashSet<string> filterPath =
    [
        "/",
        "/service/health-check",
        "/cluster/health-check",
        "/job/failover-publish",
        "/favicon.ico"
    ];

    private static readonly string[] filterEndWithPath =
    [
        "/test-status",
        "/last-instance-id",
        "/long-polling"
    ];

    ////private static readonly string[] filterStartWithPath = new[]
    ////{
    ////    "/job/running-instance/",
    ////};

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

            if (Array.Exists(filterEndWithPath, stringValue.EndsWith))
            {
                return false;
            }

            ////if (Array.Exists(filterStartWithPath, stringValue.StartsWith))
            ////{
            ////    return false;
            ////}

            if (stringValue.Contains(SwaggerRequestPath))
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            if (stringValue.StartsWith(ContentRequestPath))
            {
                return false;
            }
        }

        return true;
    }
}