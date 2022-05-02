using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planar
{
    internal class JobExecutionMetadata
    {
        public StringBuilder Information { get; private set; } = new();

        public List<ExceptionDto> Exceptions { get; private set; } = new();

        public int? EffectedRows { get; set; }

        public byte Progress { get; set; }

        public string GetExceptionsText()
        {
            if (Exceptions == null || Exceptions.Any() == false)
            {
                return null;
            }

            if (Exceptions.Count == 1)
            {
                return Exceptions.First().ExceptionText;
            }

            var seperator = string.Empty.PadLeft(80, '-');
            var sb = new StringBuilder();
            sb.AppendLine($"There is {Exceptions.Count} aggregate exception");
            Exceptions.ForEach(e => sb.AppendLine($"  - {e.Message}"));
            sb.AppendLine(seperator);
            Exceptions.ForEach(e =>
            {
                sb.AppendLine(e.ExceptionText);
                sb.AppendLine(seperator);
            });

            return sb.ToString();
        }

        public string GetInformation()
        {
            return Information?.ToString();
        }
    }

    internal class ExceptionDto
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