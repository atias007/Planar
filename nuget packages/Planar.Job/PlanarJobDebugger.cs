using Planar.Common;
using System;
using System.Collections.Generic;

namespace Planar.Job
{
    public sealed class PlanarJobDebugger
    {
        private readonly Dictionary<string, IExecuteJobProperties> _profiles = new Dictionary<string, IExecuteJobProperties>();
        internal Dictionary<string, IExecuteJobProperties> Profiles => _profiles;

        internal PlanarJobDebugger()
        {
            AddDefaultProfile();
        }

        internal void AddDefaultProfile()
        {
            _profiles.Add("Default Profile", new ExecuteJobPropertiesBuilder().SetDevelopmentEnvironment().Build());
        }

        public void AddProfile(string name, Action<IExecuteJobPropertiesBuilder> builderAction)
        {
            if (PlanarJob.Mode == RunningMode.Release) { return; }

            if (_profiles.ContainsKey(name))
            {
                throw new PlanarJobException($"Debug profile '{name}' already exists");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PlanarJobException("Debug profile name is empty");
            }

            if (name.Length > 50)
            {
                throw new PlanarJobException($"Debug profile name has {name.Length} chars. Maximum allowed chars is 50");
            }

            var builder = new ExecuteJobPropertiesBuilder().SetDevelopmentEnvironment();
            builderAction(builder);
            var properties = builder.Build();
            _profiles.Add(name, properties);
        }
    }
}