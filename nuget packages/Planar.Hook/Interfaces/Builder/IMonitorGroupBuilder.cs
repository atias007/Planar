namespace Planar.Hook
{
    public interface IMonitorGroupBuilder
    {
        IMonitorGroupBuilder WithName(string name);

        IMonitorGroupBuilder WithAdditionalField1(string additionalField);

        IMonitorGroupBuilder WithAdditionalField2(string additionalField);

        IMonitorGroupBuilder WithAdditionalField3(string additionalField);

        IMonitorGroupBuilder WithAdditionalField4(string additionalField);

        IMonitorGroupBuilder WithAdditionalField5(string additionalField);

        IMonitorGroup Build();
    }
}