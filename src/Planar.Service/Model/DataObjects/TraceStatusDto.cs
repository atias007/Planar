namespace Planar.Service.Model.DataObjects
{
    public class TraceStatusDto
    {
        public int Fatal { get; set; }
        public int Error { get; set; }
        public int Warning { get; set; }
        public int Information { get; set; }
        public int Debug { get; set; }
    }
}