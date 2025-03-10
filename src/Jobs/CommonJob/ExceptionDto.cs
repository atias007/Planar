﻿using System;

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
#endif
    }
}