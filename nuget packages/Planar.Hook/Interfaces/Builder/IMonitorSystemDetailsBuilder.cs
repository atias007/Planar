namespace Planar.Hook
{
    public interface IMonitorSystemDetailsBuilder : IMonitorBuilder<IMonitorSystemDetailsBuilder>
    {
        IMonitorSystemDetails Build();

        IMonitorSystemDetailsBuilder WithMessageTemplate(string messageTemplate);

        IMonitorSystemDetailsBuilder WithMessage(string message);

#if NETSTANDARD2_0

        IMonitorSystemDetailsBuilder AddMessageParameter(string key, string value);

#else
        IMonitorSystemDetailsBuilder AddMessageParameter(string key, string? value);
#endif
    }
}