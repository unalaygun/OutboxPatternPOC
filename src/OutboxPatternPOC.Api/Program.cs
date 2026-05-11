using Microsoft.EntityFrameworkCore;
using OutboxPatternPOC.Api.Data;
using OutboxPatternPOC.Api.Endpoints;
using OutboxPatternPOC.Api.Services;
using OutboxPatternPOC.Api.Workers;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IRedisPublisher, RedisPublisher>();
builder.Services.AddHostedService<OutboxWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapOrderEndpoints();

app.Run();

public partial class Program { }
