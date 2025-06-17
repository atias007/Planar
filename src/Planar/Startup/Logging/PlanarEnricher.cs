using Serilog.Core;
using Serilog.Events;
using System.Runtime.CompilerServices;
using System;
using System.Threading;

namespace Planar.Startup.Logging
{
    public class PlanarEnricher : ILogEventEnricher
    {
        private static LogEventProperty _applicationProperty;
        private static LogEventProperty _machineNameProperty;

        private const string MachineNamePropertyName = "MachineName";

        private static readonly Lock _locker = new();

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var prop1 = GetApplicationProperty(propertyFactory);
            logEvent.AddPropertyIfAbsent(prop1);

            var prop2 = GetMachineNameProperty(propertyFactory);
            logEvent.AddPropertyIfAbsent(prop2);
        }

        private static LogEventProperty GetApplicationProperty(ILogEventPropertyFactory propertyFactory)
        {
            lock (_locker)
            {
                _applicationProperty ??= CreateApplicationProperty(propertyFactory);
                return _applicationProperty;
            }
        }

        private static LogEventProperty GetMachineNameProperty(ILogEventPropertyFactory propertyFactory)
        {
            lock (_locker)
            {
                _machineNameProperty ??= CreateMachineNameProperty(propertyFactory);
                return _machineNameProperty;
            }
        }

        // Qualify as uncommon-path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static LogEventProperty CreateApplicationProperty(ILogEventPropertyFactory propertyFactory)
        {
            const string applicationNameValue = nameof(Planar);
            const string applicationNamePropertyName = "Application";

            return propertyFactory.CreateProperty(applicationNamePropertyName, applicationNameValue);
        }

        // Qualify as uncommon-path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static LogEventProperty CreateMachineNameProperty(ILogEventPropertyFactory propertyFactory)
        {
            var machineName = Environment.MachineName;
            return propertyFactory.CreateProperty(MachineNamePropertyName, machineName);
        }
    }
}