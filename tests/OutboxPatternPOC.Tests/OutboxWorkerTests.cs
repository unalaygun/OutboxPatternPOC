using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OutboxPatternPOC.Api.Data;
using OutboxPatternPOC.Api.Models;
using OutboxPatternPOC.Api.Services;
using OutboxPatternPOC.Api.Workers;
using Microsoft.Extensions.Configuration;

namespace OutboxPatternPOC.Tests;

public class OutboxWorkerTests
{
    private static (IServiceScopeFactory scopeFactory, AppDbContext dbContext) CreateServices(
        IRedisPublisher? publisher = null)
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var services = new ServiceCollection();
        
        // Add DbContext with same name
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        if (publisher != null)
            services.AddScoped(_ => publisher);
        else
            services.AddScoped(_ => Mock.Of<IRedisPublisher>());

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        return (scopeFactory, dbContext);
    }

    [Fact]
    public async Task ProcessOutboxMessages_ShouldMarkMessagesAsProcessed()
    {
        var mockPublisher = new Mock<IRedisPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var (scopeFactory, dbContext) = CreateServices(mockPublisher.Object);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            Payload = "{\"test\":true}",
            CreatedAt = DateTime.UtcNow,
            Processed = false
        });
        await dbContext.SaveChangesAsync();

        var logger = Mock.Of<ILogger<OutboxWorker>>();
        var worker = new OutboxWorker(scopeFactory, logger);

        await worker.ProcessOutboxMessages(CancellationToken.None);

        var message = await dbContext.OutboxMessages.AsNoTracking().FirstAsync();
        message.Processed.Should().BeTrue();
        message.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessOutboxMessages_ShouldCallPublishForEachMessage()
    {
        var mockPublisher = new Mock<IRedisPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var (scopeFactory, dbContext) = CreateServices(mockPublisher.Object);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            Payload = "{\"id\":1}",
            CreatedAt = DateTime.UtcNow,
            Processed = false
        });
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            Payload = "{\"id\":2}",
            CreatedAt = DateTime.UtcNow,
            Processed = false
        });
        await dbContext.SaveChangesAsync();

        var logger = Mock.Of<ILogger<OutboxWorker>>();
        var worker = new OutboxWorker(scopeFactory, logger);

        await worker.ProcessOutboxMessages(CancellationToken.None);

        mockPublisher.Verify(p => p.PublishAsync("outbox-stream", It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessOutboxMessages_WhenPublishFails_ShouldNotMarkAsProcessed()
    {
        var mockPublisher = new Mock<IRedisPublisher>();
        mockPublisher.Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Redis connection failed"));

        var (scopeFactory, dbContext) = CreateServices(mockPublisher.Object);

        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            Payload = "{\"fail\":true}",
            CreatedAt = DateTime.UtcNow,
            Processed = false
        });
        await dbContext.SaveChangesAsync();

        var logger = Mock.Of<ILogger<OutboxWorker>>();
        var worker = new OutboxWorker(scopeFactory, logger);

        await worker.ProcessOutboxMessages(CancellationToken.None);

        var message = await dbContext.OutboxMessages.AsNoTracking().FirstAsync();
        message.Processed.Should().BeFalse();
        message.ProcessedAt.Should().BeNull();
    }
}
