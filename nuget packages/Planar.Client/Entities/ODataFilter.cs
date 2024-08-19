namespace Planar.Client.Entities
{
    public class ODataFilter
    {
        public string? Filter { get; set; }

        public string? OrderBy { get; set; }

        public int? Top { get; set; }

        public int? Skip { get; set; }

        public string? Select { get; set; }
    }
}