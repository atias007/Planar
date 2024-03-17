using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Audit
{
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
                while (await channel.Writer.WaitToWriteAsync().ConfigureAwait(false))
                {
                    if (channel.Writer.TryWrite(message)) { break; }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "fail to publish security message. the message: {@Message}", message);
            }
        }
    }
}