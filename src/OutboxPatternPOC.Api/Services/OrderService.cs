using System.Text.Json;
using OutboxPatternPOC.Api.Data;
using OutboxPatternPOC.Api.Models;

namespace OutboxPatternPOC.Api.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;

    public OrderService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order> CreateOrderAsync(string customerName, decimal totalAmount)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow
        };

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "OrderCreated",
            Payload = JsonSerializer.Serialize(new { order.Id, order.CustomerName, order.TotalAmount }),
            CreatedAt = DateTime.UtcNow,
            Processed = false
        };

        _dbContext.Orders.Add(order);
        _dbContext.OutboxMessages.Add(outboxMessage);
        await _dbContext.SaveChangesAsync();

        return order;
    }
}
