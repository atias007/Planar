using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Planner
{
    internal class JobExecutionMetadata
    {
        private StringBuilder _information = new();

        private List<Exception> _exceptions = new();

        public void AppendInformation(string info)
        {
            _information.AppendLine(info);
        }

        public string Information
        {
            get
            {
                return _information.ToString();
            }
            set
            {
                _information = new StringBuilder(value);
            }
        }

        public void AddAggragateException(Exception ex)
        {
            _exceptions.Add(ex);
        }

        public IEnumerable<Exception> Exceptions
        {
            get
            {
                return _exceptions;
            }
            set
            {
                _exceptions = value.ToList();
            }
        }

        public int? EffectedRows { get; set; }

        public byte Progress { get; set; }

        public string GetExceptionsText()
        {
            var exceptionsText = _exceptions == null || _exceptions.Any() == false ?
                            null :
                            new AggregateException(_exceptions).ToString();

            return exceptionsText;
        }

        public static JobExecutionMetadata GetInstance(IJobExecutionContext context)
        {
            if (context.Result is JobExecutionMetadata metadata)
            {
                return metadata;
            }

            if (context.Result != null)
            {
                var json = JsonSerializer.Serialize(context.Result);
                metadata = JsonSerializer.Deserialize<JobExecutionMetadata>(json);
                return metadata;
            }

            metadata = new JobExecutionMetadata();
            context.Result = metadata;
            return metadata;
        }
    }
}