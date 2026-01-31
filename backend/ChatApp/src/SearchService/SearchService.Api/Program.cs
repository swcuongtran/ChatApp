using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.IdentityModel.Tokens;
using Nest;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SearchService.Api.Model;
using SearchService.Api.Services;
using SearchService.Api.Workers;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
// Add services to the container.
builder.Services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();


var settings = new ConnectionSettings(new Uri(builder.Configuration["Elasticsearch:Uri"]!))
    .DefaultIndex("chat_messages");

var client = new ElasticClient(settings);

var indexName = "messages";
if (!client.Indices.Exists(indexName).Exists)
{
    client.Indices.Create(indexName, c => c
        .Map<MessageDoc>(m => m
            .AutoMap()
            .Properties(p => p
                .DenseVector(dv => dv
                    .Name(n => n.Embedding)
                    .Dimensions(768) 
                )
            )
        )
    );
}
builder.Services.AddSingleton<IElasticClient>(client);

builder.Services.AddHostedService<SearchConsumer>();

var serviceName = "SearchService";
var resource = ResourceBuilder.CreateDefault().AddService(serviceName);
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t
        .SetResourceBuilder(resource)
        .AddSource(serviceName)
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());


builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var elastic = app.Services.GetRequiredService<IElasticClient>();
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var esClient = scope.ServiceProvider.GetRequiredService<IElasticClient>();

    for (int i = 0; i < 10; i++)
    {
        try
        {
            var exists = esClient.Indices.Exists("chat_messages");
            if (!exists.IsValid && exists.ServerError == null) throw new Exception("ES not ready");

            if (!exists.Exists)
            {
                logger.LogInformation("Creating Index...");
                esClient.Indices.Create("chat_messages", c => c.Map<SearchService.Api.Model.MessageDoc>(m => m.AutoMap()));
            }
            break; 
        }
        catch
        {
            logger.LogWarning("Waiting for Elasticsearch... ({Attempt}/10)", i + 1);
            System.Threading.Thread.Sleep(5000);
        }
    }
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
