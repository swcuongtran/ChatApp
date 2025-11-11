using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using ChatService.Application.Abstractions;
using ChatService.Application.Conversations;
using ChatService.Application.Messages;
using ChatService.Infrastructure;
using ChatService.Infrastructure.Messaging;
using ChatService.Infrastructure.Outbox;
using ChatService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Utils.Correlation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var serviceName = "ChatService";
var resource = ResourceBuilder.CreateDefault().AddService(serviceName);

builder.Services.AddOpenTelemetry()
   .ConfigureResource(r => r.AddService(serviceName))
   .WithMetrics(m => m
       .SetResourceBuilder(resource)
       .AddAspNetCoreInstrumentation()
       .AddOtlpExporter())
   .WithTracing(t => t
       .SetResourceBuilder(resource)
       .AddAspNetCoreInstrumentation(o => o.RecordException = true)
       .AddOtlpExporter());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdProvider, HttpCorrelationIdProvider>();

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
builder.Services.AddScoped<CreateConversationHandler>();
builder.Services.AddScoped<RenameConversationHandler>();
builder.Services.AddScoped<AddConversationMemberHandler>();
builder.Services.AddScoped<RemoveConversationMemberHandler>();

builder.Services.AddScoped<IOutboxStore, EfOutboxStore>();
builder.Services.AddHostedService<OutboxDispatcher>();

builder.Services.Configure<KafkaOptions>(opt =>
{
    opt.Broker = Environment.GetEnvironmentVariable("KAFKA_BROKERS") ?? "localhost:9092";
    opt.ClientId = Environment.GetEnvironmentVariable("KAFKA_CLIENT_ID") ?? "chatservice-api";
});

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<KafkaOptions>>().Value);

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
