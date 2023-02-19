using Planar.Job.Test.Common;

namespace Planar.Job.Test.Assert
{
    public class DataConstraint : BasePlanarConstraint
    {
        internal DataConstraint(IJobExecutionResult result) : base(result)
        {
        }

        public AssertPlanarConstraint ContainsKey(string key)
        {
            if (_result.Data.ContainsKey(key)) { return Assert; }
            var message = $"Expect data contains key '{key}' but key not found";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint NotContainsKey(string key)
        {
            if (!_result.Data.ContainsKey(key)) { return Assert; }
            var message = $"Expect data not to contains key '{key}' but key was found";
            throw new AssertPlanarException(message);
        }

        public ValueConstraint Key(string key)
        {
            return new ValueConstraint(_result, key);
        }
    }
}