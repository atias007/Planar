using Planar;
using Quartz;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonJob
{
    public class JobExecutionMetadata
    {
        public StringBuilder Information { get; set; } = new();

        public List<ExceptionDto> Exceptions { get; set; } = new();

        public int? EffectedRows { get; set; }

        public byte Progress { get; set; }

        private static readonly object Locker = new();

        public string GetInformation()
        {
            return Information.ToString();
        }

        public string GetExceptionsText()
        {
            var exceptions = Exceptions;
            if (exceptions == null || exceptions.Any() == false)
            {
                return null;
            }

            if (exceptions.Count == 1)
            {
                return exceptions.First().ExceptionText;
            }

            var seperator = string.Empty.PadLeft(80, '-');
            var sb = new StringBuilder();
            sb.AppendLine($"There is {exceptions.Count} aggregate exception");
            exceptions.ForEach(e => sb.AppendLine($"  - {e.Message}"));
            sb.AppendLine(seperator);
            exceptions.ForEach(e =>
            {
                sb.AppendLine(e.ExceptionText);
                sb.AppendLine(seperator);
            });

            return sb.ToString();
        }

        public static JobExecutionMetadata GetInstance(IJobExecutionContext context)
        {
            lock (Locker)
            {
                var result = context.Result;
                if (result == null)
                {
                    result = new JobExecutionMetadata();
                    context.Result = result;
                }

                return result as JobExecutionMetadata;
            }
        }
    }
}