using System;
using System.Threading;

namespace Planar.Common
{
    internal sealed class JobInstanceInfo : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public JobInstanceInfo(string fireInstanceId)
        {
            FireInstanceId = fireInstanceId;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public string FireInstanceId { get; private set; }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}