using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PlanarJob.Notify;

public class NotifyProducer
{
    private readonly Channel<NotifyMessage> _channel;
    private readonly ILogger<NotifyProducer> _logger;

    public NotifyProducer(Channel<NotifyMessage> channel, ILogger<NotifyProducer> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public void Publish(NotifyMessage message)
    {
        _ = SafePublishInner(message);
    }

    private async Task SafePublishInner(NotifyMessage message)
    {
        try
        {
            while (await _channel.Writer.WaitToWriteAsync().ConfigureAwait(false))
            {
                if (_channel.Writer.TryWrite(message)) { break; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "fail to publish notify message. the message: {@Message}", message);
        }
    }
}