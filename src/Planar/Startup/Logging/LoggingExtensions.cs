using Serilog;
using Serilog.Configuration;
using System;

namespace Planar.Startup.Logging
{
    public static class LoggingExtensions
    {
        public static LoggerConfiguration WithPlanarEnricher(this LoggerEnrichmentConfiguration enrich)
        {
            return enrich == null ? throw new ArgumentNullException(nameof(enrich)) : enrich.With<PlanarEnricher>();
        }

        public static LoggerConfiguration WithPlanarFilter(this LoggerFilterConfiguration config)
        {
            return config == null ? throw new ArgumentNullException(nameof(config)) : config.With(new PlanarFilter());
        }
    }
}