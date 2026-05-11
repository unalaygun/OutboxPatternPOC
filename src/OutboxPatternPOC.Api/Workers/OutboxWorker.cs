using Microsoft.EntityFrameworkCore;
using OutboxPatternPOC.Api.Data;
using OutboxPatternPOC.Api.Services;

namespace OutboxPatternPOC.Api.Workers;

public class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxWorker> _logger;
    private readonly TimeSpan _pollingInterval;
    private const string StreamKey = "outbox-stream";

    public OutboxWorker(
        IServiceScopeFactory scopeFactory, 
        ILogger<OutboxWorker> logger,
        IConfiguration? configuration = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        var intervalMs = configuration?.GetValue<int>("OutboxWorker:IntervalMs") ?? 5000;
        _pollingInterval = TimeSpan.FromMilliseconds(intervalMs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxWorker started with interval {Interval}ms", _pollingInterval.TotalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessOutboxMessages(stoppingToken);
            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    public async Task ProcessOutboxMessages(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRedisPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => !m.Processed)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(stoppingToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(StreamKey, message.Payload);
                message.Processed = true;
                message.ProcessedAt = DateTime.UtcNow;
                _logger.LogInformation("Published outbox message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
            }
        }

        if (messages.Count > 0)
            await dbContext.SaveChangesAsync(stoppingToken);
    }
}
