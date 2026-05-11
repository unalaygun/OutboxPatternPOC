using StackExchange.Redis;

namespace OutboxPatternPOC.Api.Services;

public class RedisPublisher : IRedisPublisher
{
    private readonly IConnectionMultiplexer _redis;

    public RedisPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task PublishAsync(string streamKey, string payload)
    {
        var db = _redis.GetDatabase();
        await db.StreamAddAsync(streamKey, "payload", payload);
    }
}
