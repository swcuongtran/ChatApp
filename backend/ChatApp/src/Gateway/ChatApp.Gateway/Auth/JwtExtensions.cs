using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ChatApp.Gateway.Auth
{
    public static class JwtExtensions
    {
        public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var authority = configuration["Auth:Authority"] ?? Environment.GetEnvironmentVariable("OIDC_AUTHORITY");
            var audience = configuration["Auth:Audience"] ?? Environment.GetEnvironmentVariable("OIDC_AUDIENCE");

            if(string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(audience))
            {
                return services;
            }
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.Authority = authority;
                    o.Audience = audience;
                    o.RequireHttpsMetadata = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = false
                    };
                    o.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            var path = ctx.HttpContext.Request.Path;
                            if ((path.StartsWithSegments("/ws/chat") || path.StartsWithSegments("/ws/call")) &&
                            ctx.Request.Query.TryGetValue("access_token", out var token))
                            {
                                ctx.Token = token;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddAuthorization();
            return services;
        }
    }
}
