namespace FolderCheck;

internal class Defaults : IFolder
{
    public int? RetryCount { get; set; } = 1;
    public TimeSpan? RetryInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int? MaximumFailsInRow { get; set; } = 5;

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}