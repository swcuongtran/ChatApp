using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using ChatService.Application.Messages;
using ChatService.Infrastructure;
using ChatService.Infrastructure.Messaging;
using ChatService.Infrastructure.Outbox;
using ChatService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddDbContext<ChatDbContext>(opt =>
{
    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
    var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "chat";
    var pass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "chat";
    var db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "chat";
    var cs = $"Host={host};Port={port};Database={db};Username={user};Password={pass}";
    opt.UseNpgsql(cs);
});

builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<SendMessageHandler>();

builder.Services.AddScoped<IOutboxStore, EfOutboxStore>();
builder.Services.AddHostedService<OutboxDispatcher>();

builder.Services.Configure<KafkaOptions>(opt =>
{
    opt.Broker = Environment.GetEnvironmentVariable("KAFKA_BROKER") ?? "localhost:9092";
    opt.ClientId = Environment.GetEnvironmentVariable("KAFKA_CLIENT_ID") ?? "chatservice-api";
});
builder.Services.AddSingleton<IEventBus, KafkaEventBus>();

builder.Services.AddHealthChecks();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.UseHealthChecks("/healthz");

app.Run();
