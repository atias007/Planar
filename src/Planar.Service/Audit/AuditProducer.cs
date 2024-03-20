using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Audit
{
    internal class AuditProducer(Channel<AuditMessage> channel, ILogger<AuditProducer> logger)
    {
        public void Publish(AuditMessage message)
        {
            _ = SafePublishInner(message);
        }

        private async Task SafePublishInner(AuditMessage message)
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
                logger.LogError(ex, "fail to publish audit message. the message: {@Message}", message);
            }
        }
    }
}