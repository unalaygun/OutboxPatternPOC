using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OutboxPatternPOC.Api.Data;
using OutboxPatternPOC.Api.Models;
using OutboxPatternPOC.Api.Services;
using StackExchange.Redis;
using Microsoft.Extensions.Hosting;

namespace OutboxPatternPOC.Tests.Integration;

public class OrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove AppDbContext and its options to avoid provider conflict
                var descriptors = services.Where(
                    d => d.ServiceType.FullName?.Contains("AppDbContext") == true ||
                         d.ServiceType == typeof(DbContextOptions)).ToList();
                
                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Remove hosted services (OutboxWorker) to prevent background processing during tests
                var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                foreach (var service in hostedServices)
                {
                    services.Remove(service);
                }

                // Remove real Redis
                var redisDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor != null) services.Remove(redisDescriptor);

                // Use InMemory DB
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTestDb"));

                // Mock Redis publisher
                services.AddScoped<IRedisPublisher>(_ => Mock.Of<IRedisPublisher>());

                // Mock IConnectionMultiplexer to prevent real Redis connection
                services.AddSingleton(Mock.Of<IConnectionMultiplexer>());
            });
        });
    }

    [Fact]
    public async Task PostOrder_ShouldReturn201()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerName = "Integration Test",
            TotalAmount = 150.00
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostOrder_ShouldReturnOrderInBody()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerName = "Body Test",
            TotalAmount = 200.00
        });

        var order = await response.Content.ReadFromJsonAsync<OutboxPatternPOC.Api.Models.Order>();
        order.Should().NotBeNull();
        order!.CustomerName.Should().Be("Body Test");
        order.TotalAmount.Should().Be(200.00m);
    }

    [Fact]
    public async Task PostOrder_ShouldPersistOrderAndOutboxMessage()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerName = "Persist Test",
            TotalAmount = 75.00
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await db.Orders.FirstOrDefaultAsync(o => o.CustomerName == "Persist Test");
        order.Should().NotBeNull();

        var outbox = await db.OutboxMessages.FirstOrDefaultAsync(m => m.Payload.Contains("Persist Test"));
        outbox.Should().NotBeNull();
        outbox!.Type.Should().Be("OrderCreated");
        outbox.Processed.Should().BeFalse();
    }
}
