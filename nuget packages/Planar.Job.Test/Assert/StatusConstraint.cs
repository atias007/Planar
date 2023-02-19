using Planar.Job.Test.Common;

namespace Planar.Job.Test
{
    public class StatusConstraint : BasePlanarConstraint
    {
        internal StatusConstraint(IJobExecutionResult result) : base(result)
        {
        }

        public AssertPlanarConstraint Fail()
        {
            if (_result.Status == StatusMembers.Fail) { return Assert; }
            var message = $"Expect status '{StatusMembers.Fail}' but status was '{_result.Status}'";
            throw new AssertPlanarException(message);
        }

        public AssertPlanarConstraint Success()
        {
            if (_result.Status == StatusMembers.Success) { return Assert; }

            var message = $"Expect status '{StatusMembers.Success}' but status was '{_result.Status}'";
            if (_result.Exception != null)
            {
                message += "Exception:\r\n{Exception}";
            }

            throw new AssertPlanarException(message);
        }
    }
}