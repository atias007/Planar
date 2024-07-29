namespace CommonJob
{
    public interface IFileJobProperties : IPathJobProperties
    {
        public string Filename { get; }
        public string? Domain { get; }
        public string? Password { get; }
        public string? UserName { get; }
    }
}