using System;

// *** DONT CHANGE TO FILENAME SCOPE NAMESPACE ===
namespace Planar
{
    public class ExceptionDto
    {
        public ExceptionDto()
        {
        }

        public ExceptionDto(Exception ex)
        {
            Message = ex.Message;
            ExceptionText = ex.ToString();
        }

#if NETSTANDARD2_0
        public string Message { get; set; } = string.Empty;
        public string ExceptionText { get; set; } = string.Empty;
#else
    public string? Message { get; set; } = string.Empty;
    public string? ExceptionText { get; set; } = string.Empty;

    public long Length => (Message?.Length ?? 0) + (ExceptionText?.Length ?? 0);
#endif
    }
}