namespace Planar.Client
{
    public class PlanarClient
    {
        private RestProxy _proxy = null!;
        private readonly Lazy<JobApi> _jobAli = new Lazy<JobApi>(isThreadSafe: true);

        public IJobApi Jobs => _jobAli.Value;
    }
}