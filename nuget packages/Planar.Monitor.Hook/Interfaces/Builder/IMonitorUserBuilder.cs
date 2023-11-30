namespace Planar.Monitor.Hook
{
    public interface IMonitorUserBuilder
    {
        IMonitorUserBuilder WithId(int id);

        IMonitorUserBuilder WithFirstName(string firstName);

        IMonitorUserBuilder WithLastName(string lastName);

        IMonitorUserBuilder WithEmailAddress1(string email);

        IMonitorUserBuilder WithEmailAddress2(string email);

        IMonitorUserBuilder WithEmailAddress3(string email);

        IMonitorUserBuilder WithAdditionalField1(string additionalField);

        IMonitorUserBuilder WithAdditionalField2(string additionalField);

        IMonitorUserBuilder WithAdditionalField3(string additionalField);

        IMonitorUserBuilder WithAdditionalField4(string additionalField);

        IMonitorUserBuilder WithAdditionalField5(string additionalField);

        IMonitorUserBuilder WithPhoneNumber1(string phoneNumber);

        IMonitorUserBuilder WithPhoneNumber2(string phoneNumber);

        IMonitorUserBuilder WithPhoneNumber3(string phoneNumber);

        IMonitorUser Build();
    }
}