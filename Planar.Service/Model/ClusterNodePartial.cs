using System;

namespace Planar.Service.Model
{
    public partial class ClusterNode
    {
        public static bool operator ==(ClusterNode a, ClusterNode b)
        {
            if (a is null && b is null) { return true; }
            if (a is null || b is null) { return false; }

            return string.Equals(a.Server, b.Server, StringComparison.CurrentCultureIgnoreCase) &&
                a.InstanceId == b.InstanceId &&
                a.Port == b.Port;
        }

        public static bool operator !=(ClusterNode a, ClusterNode b)
        {
            return !(a == b);
        }
    }
}