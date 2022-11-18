using Serilog.Core;
using Serilog.Events;

namespace Planar.Startup.Logging
{
    public class PlanarEnricher : ILogEventEnricher
    {
        private static LogEventProperty _property;
        private static readonly object _locker = new();

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var prop = GetProperty(propertyFactory);
            logEvent.AddPropertyIfAbsent(prop);
        }

        private static LogEventProperty GetProperty(ILogEventPropertyFactory propertyFactory)
        {
            const string applicationNameValue = nameof(Planar);
            const string applicationNameKey = "Application";

            lock (_locker)
            {
                _property ??= propertyFactory.CreateProperty(applicationNameKey, applicationNameValue);
                return _property;
            }
        }
    }
}