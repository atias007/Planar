namespace Redis;

internal interface IRedisKey
{
    int? Database { get; }
    string Key { get; }
}