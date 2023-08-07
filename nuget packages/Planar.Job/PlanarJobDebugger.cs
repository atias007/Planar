using Planar.Common;
using System;
using System.Collections.Generic;

namespace Planar.Job
{
    public class PlanarJobDebugger
    {
        private readonly Dictionary<string, IExecuteJobProperties> _profiles = new Dictionary<string, IExecuteJobProperties>();
        internal Dictionary<string, IExecuteJobProperties> Profiles => _profiles;

        internal PlanarJobDebugger()
        {
        }

        public void AddProfile<T>(string name, Action<ExecuteJobPropertiesBuilder> builderAction)
            where T : class, new()
        {
            if (_profiles.ContainsKey(name))
            {
                throw new PlanarJobException($"Debug profile '{name}' already exists");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new PlanarJobException("Debug profile name is empty");
            }

            if (name.Length > 20)
            {
                throw new PlanarJobException($"Debug profile name has {name.Length} chars. Maximum allowed chars is 20");
            }

            var builder = ExecuteJobPropertiesBuilder.CreateBuilderForJob<T>();
            builderAction(builder);
            var properties = builder.Build();
            _profiles.Add(name, properties);
        }
    }
}