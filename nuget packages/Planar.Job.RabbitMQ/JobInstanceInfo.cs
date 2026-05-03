using System;
using System.Threading;

namespace Planar.Job.RabbitMQ
{
    internal sealed class JobInstanceInfo : IDisposable
    {
        public JobInstanceInfo(string fireInstanceId, CancellationToken cancellationToken)
        {
            FireInstanceId = fireInstanceId;
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public string FireInstanceId { get; private set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public void Dispose()
        {
            CancellationTokenSource.Dispose();
        }

        public void Cancel()
        {
            CancellationTokenSource.Cancel();
        }
    }
}