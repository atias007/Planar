namespace Planar.Hook
{
    internal class MonitorGroupBuilder : IMonitorGroupBuilder
    {
        private readonly Group _group = new Group();

        internal MonitorGroupBuilder()
        {
            _group = new Group
            {
                AdditionalField1 = "Test AdditionalField 1",
                Id = 1,
                Name = "Test Group"
            };
        }

        public IMonitorGroup Build()
        {
            return _group;
        }

        public IMonitorGroupBuilder WithAdditionalField1(string additionalField)
        {
            _group.AdditionalField1 = additionalField;
            return this;
        }

        public IMonitorGroupBuilder WithAdditionalField2(string additionalField)
        {
            _group.AdditionalField2 = additionalField;
            return this;
        }

        public IMonitorGroupBuilder WithAdditionalField3(string additionalField)
        {
            _group.AdditionalField3 = additionalField;
            return this;
        }

        public IMonitorGroupBuilder WithAdditionalField4(string additionalField)
        {
            _group.AdditionalField4 = additionalField;
            return this;
        }

        public IMonitorGroupBuilder WithAdditionalField5(string additionalField)
        {
            _group.AdditionalField5 = additionalField;
            return this;
        }

        public IMonitorGroupBuilder WithName(string name)
        {
            _group.Name = name;
            return this;
        }
    }
}