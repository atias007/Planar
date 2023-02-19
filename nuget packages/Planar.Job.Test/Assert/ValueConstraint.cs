using Planar.Job.Test.Common;
using System;

namespace Planar.Job.Test.Assert
{
    public class ValueConstraint : BasePlanarConstraint
    {
        private readonly string _key;

        internal ValueConstraint(IJobExecutionResult result, string key) : base(result)
        {
            _key = key;
        }

        public AssertPlanarConstraint EqualsTo(object value)
        {
            if (!_result.Data.ContainsKey(_key))
            {
                var message = $"Expect data contains key '{_key}' but key not found";
                throw new AssertPlanarException(message);
            }

            var stringValue = Convert.ToString(value);
            var currentValue = _result.Data[_key];

            if (stringValue == currentValue) { return Assert; }

            var message2 = $"Expect data with key '{_key}' to have '{stringValue}' value but key have '{currentValue}' value";
            throw new AssertPlanarException(message2);
        }

        public AssertPlanarConstraint IsNull()
        {
            if (!_result.Data.ContainsKey(_key))
            {
                var message = $"Expect data contains key '{_key}' but key not found";
                throw new AssertPlanarException(message);
            }

            var currentValue = _result.Data[_key];

            if (currentValue == null) { return Assert; }

            var message2 = $"Expect data with key '{_key}' to have null value but key have '{currentValue}' value";
            throw new AssertPlanarException(message2);
        }

        public AssertPlanarConstraint IsNotNull()
        {
            if (!_result.Data.ContainsKey(_key))
            {
                var message = $"Expect data contains key '{_key}' but key not found";
                throw new AssertPlanarException(message);
            }

            var currentValue = _result.Data[_key];

            if (currentValue != null) { return Assert; }

            var message2 = $"Expect data with key '{_key}' not to have null value but key have null value";
            throw new AssertPlanarException(message2);
        }

        public AssertPlanarConstraint NotEqualsTo(object value)
        {
            if (!_result.Data.ContainsKey(_key))
            {
                var message = $"Expect data contains key '{_key}' but key not found";
                throw new AssertPlanarException(message);
            }

            var stringValue = Convert.ToString(value);
            var currentValue = _result.Data[_key];

            if (stringValue != currentValue) { return Assert; }

            var message2 = $"Expect data with key '{_key}' to have value different from '{stringValue}' but key have '{currentValue}' value";
            throw new AssertPlanarException(message2);
        }
    }
}