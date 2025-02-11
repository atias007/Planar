using System;

namespace Planar.Hook
{
    public class MonitorSystemDetailsBuilder : IMonitorSystemDetailsBuilder
    {
        private readonly MonitorSystemDetails _monitorDetails = new MonitorSystemDetails();
        private const string development = "Development";
        private const string unitTest = "UnitTest";

#if NETSTANDARD2_0

        public IMonitorSystemDetailsBuilder AddGlobalConfig(string key, string value)
#else
        public IMonitorSystemDetailsBuilder AddGlobalConfig(string key, string? value)
#endif
        {
            _monitorDetails.AddGlobalConfig(key, value);
            return this;
        }

        public IMonitorSystemDetailsBuilder AddTestUser()
        {
            _monitorDetails.AddUser(new MonitorUserBuilder().Build());
            return this;
        }

        public IMonitorSystemDetailsBuilder AddUsers(Action<IMonitorUserBuilder> groupBuilder)
        {
            var builder = new MonitorUserBuilder();
            groupBuilder(builder);
            var user = builder.Build();
            _monitorDetails.AddUser(user);
            return this;
        }

        public IMonitorSystemDetails Build()
        {
            return _monitorDetails;
        }

        public IMonitorSystemDetailsBuilder WithEventId(int eventId)
        {
            _monitorDetails.EventId = eventId;
            return this;
        }

        public IMonitorSystemDetailsBuilder WithEventTitle(string eventTitle)
        {
            _monitorDetails.EventTitle = eventTitle;
            return this;
        }

        public IMonitorSystemDetailsBuilder WithException(Exception ex)
        {
            _monitorDetails.Exception = ex?.ToString();
            return this;
        }

        public IMonitorSystemDetailsBuilder WithGroup(Action<IMonitorGroupBuilder> groupBuilder)
        {
            var builder = new MonitorGroupBuilder();
            groupBuilder(builder);
            var group = builder.Build();
            _monitorDetails.Group = group;
            return this;
        }

        public IMonitorSystemDetailsBuilder WithMonitorTitle(string monitorTitle)
        {
            _monitorDetails.MonitorTitle = monitorTitle;
            return this;
        }

        public IMonitorSystemDetailsBuilder WithMostInnerException(Exception ex)
        {
            _monitorDetails.MostInnerException = ex?.ToString();
            return this;
        }

        public IMonitorSystemDetailsBuilder WithMostInnerExceptionMessage(string message)
        {
            _monitorDetails.MostInnerExceptionMessage = message;
            return this;
        }

#if NETSTANDARD2_0

        public IMonitorSystemDetailsBuilder AddMessageParameter(string key, string value)
#else
        public IMonitorSystemDetailsBuilder AddMessageParameter(string key, string? value)
#endif
        {
            _monitorDetails.AddMessageParameter(key, value);
            return this;
        }

        public IMonitorSystemDetailsBuilder WithMessage(string message)
        {
            _monitorDetails.Message = message;
            return this;
        }

        public IMonitorSystemDetailsBuilder WithMessageTemplate(string messageTemplate)
        {
            _monitorDetails.MessageTemplate = messageTemplate;
            return this;
        }

        public IMonitorSystemDetailsBuilder WithEnvironment(string environment)
        {
            _monitorDetails.Environment = environment;
            return this;
        }

        public IMonitorSystemDetailsBuilder SetDevelopmentEnvironment()
        {
            return WithEnvironment(development);
        }

        public IMonitorSystemDetailsBuilder SetUnitTestEnvironment()
        {
            return WithEnvironment(unitTest);
        }
    }
}