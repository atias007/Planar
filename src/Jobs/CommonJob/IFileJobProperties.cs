namespace CommonJob
{
    public interface IFileJobProperties : IPathJobProperties
    {
        public string? Filename { get; }
    }
}