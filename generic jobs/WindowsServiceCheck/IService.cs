namespace WindowsServiceCheck;

internal interface IService
{
    int? RetryCount { get; }
    int? MaximumFailsInRow { get; }
    TimeSpan? RetryInterval { get; }
}