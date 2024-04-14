namespace RabbitMQCheck;

internal interface IRabbitMQ
{
    int? RetryCount { get; set; }
    int? MaximumFailsInRow { get; set; }
    TimeSpan? RetryInterval { get; set; }
}