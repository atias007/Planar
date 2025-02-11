namespace Planar.Client.Entities
{
    public class MaxConcurrentExecution
    {
        public int Value { get; set; }

        public int Maximum { get; set; }

#if NETSTANDARD2_0
        public string Status { get; set; }

#else
        public string Status { get; set; } = null!;

#endif
    }
}