namespace WindowsServiceRestart;

internal interface IService
{
    int? RetryCount { get; }
    TimeSpan? RetryInterval { get; }
}