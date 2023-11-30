namespace Planar.Monitor.Hook
{
    internal class MonitorUserBuilder : IMonitorUserBuilder
    {
        private readonly User _user = new User();

        internal static IMonitorUser Default
        {
            get
            {
                var user = new User
                {
                    AdditionalField1 = "Test AdditionalField 1",
                    AdditionalField2 = "Test AdditionalField 2",
                    AdditionalField3 = "Test AdditionalField 3",
                    AdditionalField4 = "Test AdditionalField 4",
                    AdditionalField5 = "Test AdditionalField 5",
                    EmailAddress1 = "TestEmail1@gmail.com",
                    EmailAddress2 = "TestEmail2@gmail.com",
                    EmailAddress3 = "TestEmail3@gmail.com",
                    FirstName = "Test First Name",
                    Id = 1,
                    LastName = "Test Last Name",
                    PhoneNumber1 = "0504567890",
                    PhoneNumber2 = "0504567891",
                    PhoneNumber3 = "0504567892",
                    Username = "Test Username"
                };

                return user;
            }
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