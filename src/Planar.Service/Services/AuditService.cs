using Microsoft.Extensions.Hosting;
using Planar.Service.Audit;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.Service.Services
{
    public class AuditService : BackgroundService
    {
        private readonly Channel<AuditMessage> _channel;

        public AuditService(Channel<AuditMessage> channel)
        {
            _channel = channel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _channel.Reader;
            while (!reader.Completion.IsCompleted && await reader.WaitToReadAsync(stoppingToken))
            {
                if (reader.TryRead(out var msg))
                {
                    Console.WriteLine(msg);
                }
            }

            _channel.Writer.TryComplete();
        }
    }
}