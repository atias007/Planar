namespace Planar.Job
{
    internal class UnitTestResult
    {
        public int? EffectedRows { get; set; }

#if NETSTANDARD2_0
        public string Log { get; set; }
#else
        public string? Log { get; set; }
#endif
    }
}