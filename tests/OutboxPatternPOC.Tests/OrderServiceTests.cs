using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OutboxPatternPOC.Api.Data;
using OutboxPatternPOC.Api.Services;

namespace OutboxPatternPOC.Tests;

public class OrderServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldCreateOrderAndOutboxMessage()
    {
        using var dbContext = CreateDbContext();
        var service = new OrderService(dbContext);

        var order = await service.CreateOrderAsync("Test Customer", 100.50m);

        order.Should().NotBeNull();
        order.CustomerName.Should().Be("Test Customer");
        order.TotalAmount.Should().Be(100.50m);

        var savedOrder = await dbContext.Orders.FirstOrDefaultAsync();
        savedOrder.Should().NotBeNull();
        savedOrder!.Id.Should().Be(order.Id);

        var outboxMessage = await dbContext.OutboxMessages.FirstOrDefaultAsync();
        outboxMessage.Should().NotBeNull();
        outboxMessage!.Type.Should().Be("OrderCreated");
        outboxMessage.Processed.Should().BeFalse();
        outboxMessage.Payload.Should().Contain(order.Id.ToString());
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldWriteBothInSingleTransaction()
    {
        using var dbContext = CreateDbContext();
        var service = new OrderService(dbContext);

        await service.CreateOrderAsync("Atomic Test", 50m);

        var orderCount = await dbContext.Orders.CountAsync();
        var outboxCount = await dbContext.OutboxMessages.CountAsync();

        orderCount.Should().Be(1);
        outboxCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldSetCreatedAtToUtcNow()
    {
        using var dbContext = CreateDbContext();
        var service = new OrderService(dbContext);
        var before = DateTime.UtcNow;

        var order = await service.CreateOrderAsync("Time Test", 25m);

        var after = DateTime.UtcNow;
        order.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
