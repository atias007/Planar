namespace Planar.API.Common.Entities;

public class JobWaitRequest
{
    public string? Group { get; set; }
    public string? Id { get; set; }

    public void Trim()
    {
        Group = Group?.Trim();
        Id = Id?.Trim();
    }
}