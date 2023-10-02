using System;

namespace Planar.Client
{
    public class PlanarClient
    {
        private readonly RestProxy _proxy = null!;
        private readonly Lazy<JobApi> _jobAli = new Lazy<JobApi>(isThreadSafe: true);

        public IJobApi Jobs => _jobAli.Value;
    }
}