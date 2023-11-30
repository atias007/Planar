namespace Planar.Monitor.Hook
{
    public interface IMonitorSystemDetailsBuilder : IMonitorBuilder<IMonitorSystemDetailsBuilder>
    {
        public IMonitorSystemDetails Build();

        IMonitorSystemDetailsBuilder WithMessageTemplate(string messageTemplate);

        IMonitorSystemDetailsBuilder WithMessage(string message);

        IMonitorSystemDetailsBuilder AddMessageParameter(string key, string? value);
    }
}