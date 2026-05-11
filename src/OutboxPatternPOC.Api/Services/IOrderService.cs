using OutboxPatternPOC.Api.Models;

namespace OutboxPatternPOC.Api.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string customerName, decimal totalAmount);
}
