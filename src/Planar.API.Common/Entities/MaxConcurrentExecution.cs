namespace Planar.API.Common.Entities
{
    public class MaxConcurrentExecution
    {
        [DisplayFormat("N0")]
        public int Value { get; set; }

        [DisplayFormat("N0")]
        public int Maximum { get; set; }

        public string Status { get; set; } = null!;
    }
}