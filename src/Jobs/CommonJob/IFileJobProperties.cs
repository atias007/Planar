namespace CommonJob;

public interface IFileJobProperties : IJobProperties, IPathJobProperties, IJobPropertiesWithFiles
{
    public string Filename { get; }
    public string? Domain { get; }
    public string? Password { get; }
    public string? UserName { get; }
}