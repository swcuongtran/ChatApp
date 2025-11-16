using Amazon.S3;
using BuildingBlock.Messaging;
using BuildingBlock.Outbox;
using FileService.Api.Infrastructure;
using FileService.Api.Services;
using FileService.Application.Abstractions;
using FileService.Application.Commands;
using FileService.Domain.Repositories;
using FileService.Infrastructure;
using FileService.Infrastructure.Outbox;
using FileService.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 1. DB & Outbox
builder.Services.AddDbContext<FileDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IOutboxStore, EfOutboxStore>();
builder.Services.AddHostedService<OutboxDispatcher>();

var awsOptions = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(awsOptions);
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IStorageService, S3StorageService>();

builder.Services.AddSingleton<IEventBus, KafkaEventBus>();
builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(UploadFileCommand).Assembly));

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
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<FileDbContext>().Database.Migrate();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
