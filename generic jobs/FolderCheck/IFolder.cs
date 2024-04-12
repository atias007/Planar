namespace FolderCheck;

internal interface IFolder
{
    int? RetryCount { get; set; }
    int? MaximumFailsInRow { get; set; }
    TimeSpan? RetryInterval { get; set; }
}