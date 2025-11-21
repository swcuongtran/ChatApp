using AnalyticsService.Api.Workers;
using AnalyticsService.Application.Queries;
using AnalyticsService.Infrastructure.MongoDb;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddHostedService<StatsConsumer>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetDailyStatsQuery).Assembly));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = config["JWT_AUTHORITY"];
        options.Audience = config["JWT_AUDIENCE"];
        options.RequireHttpsMetadata = false;

        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            NameClaimType = "sub",
            ValidIssuers = new[]
            {
                options.Authority,
                "http://localhost:8082/realms/chatapp"
            },
            ValidateAudience = true,
            ValidAudiences = new[] { "account", "chatapp-api" },
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
