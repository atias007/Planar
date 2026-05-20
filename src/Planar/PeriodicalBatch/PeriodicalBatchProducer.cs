using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Planar.PeriodicalBatch;

public class PeriodicalBatchProducer<T>(Channel<T> channel, ILogger<PeriodicalBatchProducer<T>> logger)
        where T : class
{
    public async Task PublishAsync(T message)
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
            logger.LogError(ex, "Fail to publish message to PeriodicalBatch ({Name})", GetType().Name);
        }
    }
}