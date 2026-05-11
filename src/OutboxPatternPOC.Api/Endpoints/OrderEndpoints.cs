using OutboxPatternPOC.Api.Services;

namespace OutboxPatternPOC.Api.Endpoints;

public static class OrderEndpoints
{
    public record CreateOrderRequest(string CustomerName, decimal TotalAmount);

    public static void MapOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/api/orders", async (CreateOrderRequest request, IOrderService orderService) =>
        {
            var order = await orderService.CreateOrderAsync(request.CustomerName, request.TotalAmount);
            return Results.Created($"/api/orders/{order.Id}", order);
        });
    }
}
