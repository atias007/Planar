using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Planar
{
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

    internal static class JobExecutionMetadataUtil
    {
        private static readonly object Locker = new();

        public static void AppendInformation(IJobExecutionContext context, string value)
        {
            lock (Locker)
            {
                var metadata = GetInstance(context);
                metadata.Information.AppendLine(value);
                SetInstance(context, metadata);
            }
        }

        public static string GetInformation(IJobExecutionContext context)
        {
            var metadata = GetInstance(context);
            return metadata.Information.ToString();
        }

        public static void AddAggragateException(IJobExecutionContext context, Exception ex)
        {
            lock (Locker)
            {
                var metadata = GetInstance(context);
                metadata.Exceptions.Add(new ExceptionDto(ex));
                SetInstance(context, metadata);
            }
        }

        public static void SetEffectedRows(IJobExecutionContext context, int? value)
        {
            lock (Locker)
            {
                var metadata = GetInstance(context);
                metadata.EffectedRows = value;
                SetInstance(context, metadata);
            }
        }

        public static void IncreaseEffectedRows(IJobExecutionContext context, int value)
        {
            lock (Locker)
            {
                var metadata = GetInstance(context);
                metadata.EffectedRows = metadata.EffectedRows.GetValueOrDefault() + value;
                SetInstance(context, metadata);
            }
        }

        public static int? GetEffectedRows(IJobExecutionContext context)
        {
            var metadata = GetInstance(context);
            return metadata.EffectedRows;
        }

        public static void SetProgress(IJobExecutionContext context, byte value)
        {
            lock (Locker)
            {
                var metadata = GetInstance(context);
                metadata.Progress = value;
                SetInstance(context, metadata);
            }
        }

        public static byte GetProgress(IJobExecutionContext context)
        {
            var metadata = GetInstance(context);
            return metadata.Progress;
        }

        public static string GetExceptionsText(IJobExecutionContext context)
        {
            var exceptions = GetInstance(context).Exceptions;
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

        internal static JobExecutionMetadata GetInstance(IJobExecutionContext context)
        {
            if (context.Result == null)
            {
                var metadata = new JobExecutionMetadata();
                return metadata;
            }

            var json = Convert.ToString(context.Result);

            try
            {
                var result = JsonConvert.DeserializeObject<JobExecutionMetadata>(json);
                return result;
            }
            catch (Exception ex)
            {
                var result = new JobExecutionMetadata();
                result.Exceptions.Add(new ExceptionDto(ex));
                return result;
            }
        }

        private static void SetInstance(IJobExecutionContext context, JobExecutionMetadata metadata)
        {
            var json = JsonConvert.SerializeObject(metadata);
            context.Result = json;
        }
    }

    internal class JobExecutionMetadata
    {
        public StringBuilder Information { get; set; } = new();

        public List<ExceptionDto> Exceptions { get; set; } = new();

        public int? EffectedRows { get; set; }

        public byte Progress { get; set; }
    }
}