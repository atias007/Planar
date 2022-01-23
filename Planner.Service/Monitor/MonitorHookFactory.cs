using System;
using System.Runtime.Loader;

namespace Planner.Service.Monitor
{
    internal class MonitorHookFactory
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public AssemblyLoadContext AssemblyContext { get; set; }
    }
}