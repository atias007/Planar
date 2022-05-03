using System;

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

        public string Message { get; set; }

        public string ExceptionText { get; set; }
    }
}