using Planar.Client.Entities;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Client
{
    public class PlanarClient
    {
        private const string Anonymous = "anonymous";

        private readonly RestProxy _proxy = new RestProxy();
        private readonly Lazy<JobApi> _jobApi;

        public PlanarClient()
        {
            _jobApi = new Lazy<JobApi>(() => new JobApi(_proxy), isThreadSafe: true);
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
                throw new PlanarException($"Invalid address {options.Host}");
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

            if (!response.IsSuccessful)
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

        public IJobApi Jobs => _jobApi.Value;
    }
}