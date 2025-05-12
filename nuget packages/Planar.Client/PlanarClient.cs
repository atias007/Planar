using Planar.Client.Api;
using Planar.Client.Entities;
using Planar.Client.Exceptions;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    public class PlanarClient : IPlanarClient
    {
        private const string Anonymous = "anonymous";

        private readonly RestProxy _proxy = new RestProxy();
        private readonly Lazy<JobApi> _jobApi;
        private readonly Lazy<HistoryApi> _historyApi;
        private readonly Lazy<TriggerApi> _triggerApi;
        private readonly Lazy<MonitorApi> _monitorApi;
        private readonly Lazy<ClusterApi> _clusterApi;
        private readonly Lazy<ConfigApi> _configApi;
        private readonly Lazy<GroupApi> _groupApi;
        private readonly Lazy<UserApi> _userApi;
        private readonly Lazy<MetricsApi> _metricsApi;
        private readonly Lazy<ReportApi> _reportApi;
        private readonly Lazy<ServiceApi> _serviceApi;

        public PlanarClient()
        {
            _jobApi = new Lazy<JobApi>(() => new JobApi(_proxy), isThreadSafe: true);
            _historyApi = new Lazy<HistoryApi>(() => new HistoryApi(_proxy), isThreadSafe: true);
            _triggerApi = new Lazy<TriggerApi>(() => new TriggerApi(_proxy), isThreadSafe: true);
            _monitorApi = new Lazy<MonitorApi>(() => new MonitorApi(_proxy), isThreadSafe: true);
            _clusterApi = new Lazy<ClusterApi>(() => new ClusterApi(_proxy), isThreadSafe: true);
            _groupApi = new Lazy<GroupApi>(() => new GroupApi(_proxy), isThreadSafe: true);
            _userApi = new Lazy<UserApi>(() => new UserApi(_proxy), isThreadSafe: true);
            _metricsApi = new Lazy<MetricsApi>(() => new MetricsApi(_proxy), isThreadSafe: true);
            _reportApi = new Lazy<ReportApi>(() => new ReportApi(_proxy), isThreadSafe: true);
            _configApi = new Lazy<ConfigApi>(() => new ConfigApi(_proxy), isThreadSafe: true);
            _serviceApi = new Lazy<ServiceApi>(() => new ServiceApi(_proxy), isThreadSafe: true);
        }

        public async Task<LoginDetails> ConnectAsync(string host, string username, string password)
        {
            var loginData = new PlanarClientConnectOptions
            {
                Host = host,
                Username = username,
                Password = password
            };

            return await ConnectAsync(loginData);
        }

        public async Task<LoginDetails> ConnectAsync(string host)
        {
            var loginData = new PlanarClientConnectOptions { Host = host };
            return await ConnectAsync(loginData);
        }

        public async Task<LoginDetails> ConnectAsync(PlanarClientConnectOptions options, CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(options.Host, UriKind.Absolute, out var uri))
            {
                throw new PlanarException($"Invalid uri address '{options.Host}'");
            }

            var loginData = new LoginData
            {
                Host = uri.Host,
                SecureProtocol = string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase),
                Port = uri.Port,
                Username = string.IsNullOrWhiteSpace(options.Username) ? null : options.Username,
                Password = string.IsNullOrWhiteSpace(options.Password) ? null : options.Password,
                Timeout = options.Timeout
            };

            var response = await _proxy.Login(loginData, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return new LoginDetails { Role = Anonymous };
            }

            if (!response.IsSuccess)
            {
                throw new PlanarException($"Login failed. Server return {response.StatusCode} status code");
            }

            return new LoginDetails
            {
                FirstName = response.Data?.FirstName ?? string.Empty,
                LastName = response.Data?.LastName,
                Role = response.Data?.Role ?? string.Empty,
            };
        }

        public IClusterApi Cluster => _clusterApi.Value;
        public IConfigApi Config => _configApi.Value;
        public IGroupApi Group => _groupApi.Value;
        public IHistoryApi History => _historyApi.Value;
        public IJobApi Job => _jobApi.Value;
        public IMetricsApi Metrics => _metricsApi.Value;
        public IMonitorApi Monitor => _monitorApi.Value;
        public IReportApi Report => _reportApi.Value;
        public IServiceApi Service => _serviceApi.Value;
        public ITriggerApi Trigger => _triggerApi.Value;
        public IUserApi User => _userApi.Value;
    }
}