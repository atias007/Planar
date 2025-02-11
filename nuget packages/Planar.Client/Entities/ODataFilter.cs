namespace Planar.Client.Entities
{
    public class ODataFilter
    {
#if NETSTANDARD2_0
        public string Filter { get; set; }
        public string OrderBy { get; set; }
        public string Select { get; set; }
#else
        public string? Filter { get; set; }
        public string? OrderBy { get; set; }
        public string? Select { get; set; }
#endif

        public int? Top { get; set; }

        public int? Skip { get; set; }
    }
}