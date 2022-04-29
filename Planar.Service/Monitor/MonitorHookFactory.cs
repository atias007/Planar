using System;
using System.Runtime.Loader;

namespace Planar.Service.Monitor
{
    internal class MonitorHookFactory
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public AssemblyLoadContext AssemblyContext { get; set; }
    }
}