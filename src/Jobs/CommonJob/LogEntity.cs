using Microsoft.Extensions.Logging;

namespace Planar
{
    public class LogEntity
    {
        public LogEntity()
        {
        }

        public LogEntity(LogLevel level, string message)
        {
            Level = level;
            Message = message;
        }

        public string Message { get; set; }

        public LogLevel Level { get; set; }
    }
}