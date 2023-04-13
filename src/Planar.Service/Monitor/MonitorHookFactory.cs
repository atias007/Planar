using System;
using System.Runtime.Loader;

namespace Planar.Service.Monitor
{
    public class MonitorHookFactory
    {
        public string Name { get; set; } = null!;

        public Type Type { get; set; } = null!;

        public AssemblyLoadContext AssemblyContext { get; set; } = null!;
    }
}