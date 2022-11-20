using Serilog;
using Serilog.Configuration;
using System;

namespace Planar.Startup.Logging
{
    public static class LoggingExtensions
    {
        public static LoggerConfiguration WithPlanarEnricher(this LoggerEnrichmentConfiguration enrich)
        {
            if (enrich == null)
            {
                throw new ArgumentNullException(nameof(enrich));
            }

            return enrich.With<PlanarEnricher>();
        }
    }
}