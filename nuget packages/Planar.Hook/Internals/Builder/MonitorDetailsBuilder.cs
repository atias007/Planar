using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Planar.Hook
{
    internal class MonitorDetailsBuilder : IMonitorDetailsBuilder
    {
        private readonly MonitorDetails _monitorDetails = new MonitorDetails();
        private const string development = "Development";
        private const string unitTest = "UnitTest";

        internal MonitorDetailsBuilder()
        {
            _monitorDetails = new MonitorDetails
            {
                Author = "Some Author",
                Calendar = "Italy",
                Durable = false,
                EventId = 103,
                EventTitle = "Execution Fail",
                Exception = "Test Exception",
                FireInstanceId = $"JobTest_{GenerateFireInstanceId()}",
                FireTime = DateTime.Now,
                JobDescription = "Test Job Description",
                JobGroup = "Test Job Group",
                JobId = "Test Job Id",
                JobName = "Test Job Name",
                JobRunTime = TimeSpan.Parse("00:12:25.723", CultureInfo.CurrentCulture),
                MonitorTitle = "Test Monitor Title",
                Recovering = false,
                TriggerGroup = "Test Trigger Group",
                TriggerId = "Test Trigger Id",
                TriggerName = "Test Trigger Name",
                MostInnerException = "Test Most Inner Exception",
                MostInnerExceptionMessage = "Test Most Inner Exception Message",
                Environment = development
            };

            _monitorDetails.AddGroup(new MonitorGroupBuilder().Build());
        }

        public IMonitorDetails Build()
        {
            return _monitorDetails;
        }

        public IMonitorDetailsBuilder AddTestUser()
        {
            var group = GetGroup();
            group.AddUser(new MonitorUserBuilder().Build());
            return this;
        }

        public IMonitorDetailsBuilder AddUsers(Action<IMonitorUserBuilder> groupBuilder)
        {
            var builder = new MonitorUserBuilder();
            groupBuilder(builder);
            var user = builder.Build();
            var group = GetGroup();
            group.AddUser(user);
            return this;
        }

        private Group GetGroup()
        {
            if (_monitorDetails.Groups.Any() && _monitorDetails.Groups.FirstOrDefault() is Group group)
            {
                return group;
            }

            // If no groups exist, create a new one
            var newGroup = (Group)new MonitorGroupBuilder().Build();
            _monitorDetails.AddGroup(newGroup);
            return newGroup;
        }

#if NETSTANDARD2_0

        public IMonitorDetailsBuilder AddDataMap(string key, string value)
#else
        public IMonitorDetailsBuilder AddDataMap(string key, string? value)
#endif
        {
            _monitorDetails.AddMergedJobDataMap(key, value);
            return this;
        }

#if NETSTANDARD2_0

        public IMonitorDetailsBuilder AddGlobalConfig(string key, string value)
#else
        public IMonitorDetailsBuilder AddGlobalConfig(string key, string? value)
#endif
        {
            _monitorDetails.AddGlobalConfig(key, value);
            return this;
        }

        public IMonitorDetailsBuilder SetDurable()
        {
            _monitorDetails.Durable = true;
            return this;
        }

        public IMonitorDetailsBuilder SetRecovering()
        {
            _monitorDetails.Recovering = true;
            return this;
        }

        public IMonitorDetailsBuilder WithAuthor(string author)
        {
            _monitorDetails.Author = author;
            return this;
        }

        public IMonitorDetailsBuilder WithCalendar(string calendar)
        {
            _monitorDetails.Calendar = calendar;
            return this;
        }

        public IMonitorDetailsBuilder WithEventId(int eventId)
        {
            _monitorDetails.EventId = eventId;
            return this;
        }

        public IMonitorDetailsBuilder WithEventTitle(string eventTitle)
        {
            _monitorDetails.EventTitle = eventTitle;
            return this;
        }

        public IMonitorDetailsBuilder WithException(Exception ex)
        {
            _monitorDetails.Exception = ex?.ToString();
            return this;
        }

        public IMonitorDetailsBuilder WithFireInstanceId(string fireInstanceId)
        {
            _monitorDetails.FireInstanceId = fireInstanceId;
            return this;
        }

        public IMonitorDetailsBuilder WithFireTime(DateTime fireTime)
        {
            _monitorDetails.FireTime = fireTime;
            return this;
        }

        public IMonitorDetailsBuilder WithGroup(Action<IMonitorGroupBuilder> groupBuilder)
        {
            var builder = new MonitorGroupBuilder();
            groupBuilder(builder);
            var group = builder.Build();
            _monitorDetails.AddGroup(group);
            return this;
        }

        public IMonitorDetailsBuilder WithJobDescription(string jobDescription)
        {
            _monitorDetails.JobDescription = jobDescription;
            return this;
        }

        public IMonitorDetailsBuilder WithJobGroup(string jobGroup)
        {
            _monitorDetails.JobGroup = jobGroup;
            return this;
        }

        public IMonitorDetailsBuilder WithJobId(string jobId)
        {
            _monitorDetails.JobId = jobId;
            return this;
        }

        public IMonitorDetailsBuilder WithJobName(string jobName)
        {
            _monitorDetails.JobName = jobName;
            return this;
        }

        public IMonitorDetailsBuilder WithJobRunTime(TimeSpan jobRunTime)
        {
            _monitorDetails.JobRunTime = jobRunTime;
            return this;
        }

        public IMonitorDetailsBuilder WithMonitorTitle(string monitorTitle)
        {
            _monitorDetails.MonitorTitle = monitorTitle;
            return this;
        }

        public IMonitorDetailsBuilder WithMostInnerException(Exception ex)
        {
            _monitorDetails.MostInnerException = ex?.ToString();
            return this;
        }

        public IMonitorDetailsBuilder WithMostInnerExceptionMessage(string message)
        {
            _monitorDetails.MostInnerExceptionMessage = message;
            return this;
        }

        public IMonitorDetailsBuilder WithTriggerGroup(string triggerGroup)
        {
            _monitorDetails.TriggerGroup = triggerGroup;
            return this;
        }

        public IMonitorDetailsBuilder WithTriggerId(string triggerId)
        {
            _monitorDetails.TriggerId = triggerId;
            return this;
        }

        public IMonitorDetailsBuilder WithTriggerName(string triggerName)
        {
            _monitorDetails.TriggerName = triggerName;
            return this;
        }

        public IMonitorDetailsBuilder WithEnvironment(string environment)
        {
            _monitorDetails.Environment = environment;
            return this;
        }

        public IMonitorDetailsBuilder SetDevelopmentEnvironment()
        {
            return WithEnvironment(development);
        }

        public IMonitorDetailsBuilder SetUnitTestEnvironment()
        {
            return WithEnvironment(unitTest);
        }

        private static string GenerateFireInstanceId()
        {
            var result = new StringBuilder();
            var offset = '0';
            for (var i = 0; i < 18; i++)
            {
#if NETSTANDARD2_0
                Random random = new Random(); // The using statement ensures proper disposal.
                var num = random.Next(offset, offset + 10);
                var @char = (char)num;
#else
                var @char = (char)RandomNumberGenerator.GetInt32(offset, offset + 10);
#endif
                result.Append(@char);
            }

            return result.ToString();
        }
    }
}