using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Audit;

public class SecurityProducer(Channel<SecurityMessage> channel, ILogger<SecurityProducer> logger)
{
    public void Publish(SecurityMessage message)
    {
        if (AppSettings.Authentication.NoAuthontication) { return; }
        _ = SafePublishInner(message);
    }

    private async Task SafePublishInner(SecurityMessage message)
    {
        try
        {
            if (!channel.Writer.TryWrite(message))
            {
                await channel.Writer.WaitToWriteAsync().ConfigureAwait(false);
                await channel.Writer.WriteAsync(message).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "fail to publish security message. the message: {@Message}", message);
        }
    }
}