using System;
using System.Collections.Generic;

namespace Planar.Hook
{
    public sealed class PlanarHookDebugger
    {
        private readonly Dictionary<string, IMonitorDetails> _monitorProfiles = new Dictionary<string, IMonitorDetails>();
        private readonly Dictionary<string, IMonitorSystemDetails> _monitorSystemProfiles = new Dictionary<string, IMonitorSystemDetails>();

        internal PlanarHookDebugger()
        {
        }

        public IMonitorDetailsBuilder CreateMonitorDetailsBuilder() => new MonitorDetailsBuilder();

        public IMonitorSystemDetailsBuilder CreateMonitorSystemDetailsBuilder() => new MonitorSystemDetailsBuilder();

        internal Dictionary<string, IMonitorDetails> MonitorProfiles => _monitorProfiles;
        internal Dictionary<string, IMonitorSystemDetails> MonitorSystemProfiles => _monitorSystemProfiles;

        public void AddMonitorProfile(string name, IMonitorDetails monitorDetails)
        {
            if (PlanarHook.Mode == RunningMode.Release) { return; }
            ValidateProfileName(name);

            _monitorProfiles.Add(name, monitorDetails);
        }

        public void AddMonitorProfile(string name, Action<IMonitorDetailsBuilder> builderAction)
        {
            if (PlanarHook.Mode == RunningMode.Release) { return; }
            ValidateProfileName(name);

            var builder = new MonitorDetailsBuilder().SetDevelopmentEnvironment();
            builderAction(builder);
            var properties = builder.Build();
            _monitorProfiles.Add(name, properties);
        }

        public void AddMonitorSystemProfile(string name, IMonitorSystemDetails monitorSystemDetails)
        {
            if (PlanarHook.Mode == RunningMode.Release) { return; }
            ValidateProfileName(name);

            _monitorSystemProfiles.Add(name, monitorSystemDetails);
        }

        public void AddMonitorSystemProfile(string name, Action<IMonitorSystemDetailsBuilder> builderAction)
        {
            if (PlanarHook.Mode == RunningMode.Release) { return; }
            ValidateProfileName(name);

            var builder = new MonitorSystemDetailsBuilder().SetDevelopmentEnvironment();
            builderAction(builder);
            var properties = builder.Build();
            _monitorSystemProfiles.Add(name, properties);
        }

        private void ValidateProfileName(string name)
        {
            if (_monitorProfiles.ContainsKey(name) || _monitorSystemProfiles.ContainsKey(name))
            {
                throw new PlanarHookException($"Debug monitor profile '{name}' already exists");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PlanarHookException("Debug monitor profile name is empty");
            }

            if (name.Length > 50)
            {
                throw new PlanarHookException($"Debug monitor profile name has {name.Length} chars. Maximum allowed chars is 50");
            }
        }
    }
}