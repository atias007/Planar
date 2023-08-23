namespace Planar.Job.Test
{
    public class AssertPlanarConstraint : BasePlanarConstraint
    {
        internal AssertPlanarConstraint(IJobExecutionResult result) : base(result)
        {
        }

        public EffectedRowsConstraint EffectedRows => new EffectedRowsConstraint(_result);
        public StatusConstraint Status => new StatusConstraint(_result);
    }
}