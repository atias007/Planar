namespace Planar.CLI.Entities
{
    public interface IPagingRequest
    {
        uint PageNumber { get; set; }

        byte PageSize { get; }
    }
}