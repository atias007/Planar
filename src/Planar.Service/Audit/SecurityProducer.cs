using Microsoft.Extensions.Logging;
using Planar.Common;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Audit
{
    public class SecurityProducer
    {
        private readonly Channel<SecurityMessage> _channel;
        private readonly ILogger<SecurityProducer> _logger;

        public SecurityProducer(Channel<SecurityMessage> channel, ILogger<SecurityProducer> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public void Publish(SecurityMessage message)
        {
            if (AppSettings.Authentication.NoAuthontication) { return; }
            _ = SafePublishInner(message);
        }

        private async Task SafePublishInner(SecurityMessage message)
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
                _logger.LogError(ex, "fail to publish security message. the message: {@Message}", message);
            }
        }
    }
}