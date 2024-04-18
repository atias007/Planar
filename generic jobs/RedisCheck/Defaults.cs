using Common;
using RedisCheck;

namespace RedisStreamCheck;

internal class Defaults : BaseDefault, IRedisDefaults
{
    public int? Database { get; set; } = 0;

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}