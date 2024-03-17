using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Monitor;

public class MonitorScanProducer(Channel<MonitorScanMessage> channel, ILogger<MonitorScanProducer> logger)
{
    public void Publish(MonitorScanMessage message, CancellationToken cancellationToken)
    {
        _ = SafePublishInner(message, cancellationToken);
    }

    public async Task PublishAsync(MonitorScanMessage message, CancellationToken cancellationToken)
    {
        await SafePublishInner(message, cancellationToken);
    }

    private async Task SafePublishInner(MonitorScanMessage message, CancellationToken cancellationToken)
    {
        try
        {
            while (await channel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                if (channel.Writer.TryWrite(message)) { break; }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to publish monitor scan message. the message: {@Message}", message);
        }
    }
}