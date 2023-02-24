namespace Planar.Job.Test
{
    public class BasePlanarConstraint
    {
        internal readonly IJobExecutionResult _result;

        internal BasePlanarConstraint(IJobExecutionResult result)
        {
            _result = result;
        }

        internal AssertPlanarConstraint Assert => new AssertPlanarConstraint(_result);
    }
}