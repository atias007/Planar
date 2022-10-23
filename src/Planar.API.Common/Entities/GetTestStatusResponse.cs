namespace Planar.API.Common.Entities
{
    public class GetTestStatusResponse
    {
        public int Status { get; set; }

        public int? EffectedRows { get; set; }

        public int? Duration { get; set; }
    }
}