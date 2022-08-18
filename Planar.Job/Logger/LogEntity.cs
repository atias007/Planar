using Microsoft.Extensions.Logging;

namespace Planar
{
    internal class LogEntity
    {
        public string Message { get; set; }

        public LogLevel Level { get; set; }
    }
}