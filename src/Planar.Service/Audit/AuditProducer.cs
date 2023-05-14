using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Audit
{
    internal class AuditProducer
    {
        private readonly Channel<AuditMessage> _channel;
        private readonly ILogger<AuditProducer> _logger;

        public AuditProducer(Channel<AuditMessage> channel, ILogger<AuditProducer> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public void Publish(AuditMessage message)
        {
            _ = SafePublishInner(message);
        }

        private async Task SafePublishInner(AuditMessage message)
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
                _logger.LogError(ex, "fail to publish audit message. the message: {@Message}", message);
            }
        }
    }
}