namespace Planar.Hook
{
    internal class MonitorUserBuilder : IMonitorUserBuilder
    {
        private readonly User _user = new User();

        internal MonitorUserBuilder()
        {
            _user = new User
            {
                EmailAddress1 = "TestEmail1@gmail.com",
                FirstName = "Israel",
                Id = 1,
                LastName = "Israeli",
                PhoneNumber1 = "0504567890",
                Username = "Test Username"
            };
        }

        public IMonitorUser Build()
        {
            return _user;
        }

        public IMonitorUserBuilder WithAdditionalField1(string additionalField)
        {
            _user.AdditionalField1 = additionalField;
            return this;
        }

        public IMonitorUserBuilder WithAdditionalField2(string additionalField)
        {
            _user.AdditionalField2 = additionalField;
            return this;
        }

        public IMonitorUserBuilder WithAdditionalField3(string additionalField)
        {
            _user.AdditionalField3 = additionalField;
            return this;
        }

        public IMonitorUserBuilder WithAdditionalField4(string additionalField)
        {
            _user.AdditionalField4 = additionalField;
            return this;
        }

        public IMonitorUserBuilder WithAdditionalField5(string additionalField)
        {
            _user.AdditionalField5 = additionalField;
            return this;
        }

        public IMonitorUserBuilder WithEmailAddress1(string email)
        {
            _user.EmailAddress1 = email;
            return this;
        }

        public IMonitorUserBuilder WithEmailAddress2(string email)
        {
            _user.EmailAddress2 = email;
            return this;
        }

        public IMonitorUserBuilder WithEmailAddress3(string email)
        {
            _user.EmailAddress3 = email;
            return this;
        }

        public IMonitorUserBuilder WithFirstName(string firstName)
        {
            _user.FirstName = firstName;
            return this;
        }

        public IMonitorUserBuilder WithId(int id)
        {
            _user.Id = id;
            return this;
        }

        public IMonitorUserBuilder WithLastName(string lastName)
        {
            _user.LastName = lastName;
            return this;
        }

        public IMonitorUserBuilder WithPhoneNumber1(string phoneNumber)
        {
            _user.PhoneNumber1 = phoneNumber;
            return this;
        }

        public IMonitorUserBuilder WithPhoneNumber2(string phoneNumber)
        {
            _user.PhoneNumber2 = phoneNumber;
            return this;
        }

        public IMonitorUserBuilder WithPhoneNumber3(string phoneNumber)
        {
            _user.PhoneNumber3 = phoneNumber;
            return this;
        }
    }
}