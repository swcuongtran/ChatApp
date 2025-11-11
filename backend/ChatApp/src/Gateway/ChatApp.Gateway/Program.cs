using ChatApp.Gateway.Auth;
using ChatApp.Gateway.Middleware;
using ChatApp.Gateway.Observability;

var builder = WebApplication.CreateBuilder(args);

//CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

//Auth
builder.Services.AddJwtAuth(builder.Configuration);

//Yarp (read yarp.json from project root)
builder.Configuration.AddJsonFile("yarp.json",optional: false, reloadOnChange: true);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

//Health Checks
builder.Services.AddHealthChecks();

//Observability
object value = builder.Services.AddOpenTelemetryExtensions(builder.Configuration);


builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

// Forwarded Headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

// Correlation ID
app.UseMiddleware<CorrelationIdMiddleware>();

//Health Endpoints
app.MapHealthChecks("/Healthz");
app.MapHealthChecks("/readyz");

//CORS
app.UseCors();

//Auth
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), appBuilder =>
{
    appBuilder.UseAuthentication();
    appBuilder.UseAuthorization();
});

//WebSockets for YARP
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
app.UseWebSockets(webSocketOptions);

//Reverse Proxy
app.MapReverseProxy();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
