namespace OutboxPatternPOC.Api.Services;

public interface IRedisPublisher
{
    Task PublishAsync(string streamKey, string payload);
}
