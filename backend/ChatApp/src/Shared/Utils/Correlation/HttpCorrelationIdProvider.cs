using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Utils.Correlation
{
    public class HttpCorrelationIdProvider : ICorrelationIdProvider
    {
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        private const string ContextItemName = "CorrelationId";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<HttpCorrelationIdProvider> _logger;

        public HttpCorrelationIdProvider(IHttpContextAccessor httpContextAccessor, ILogger<HttpCorrelationIdProvider> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string TraceId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var traceId = Activity.Current?.TraceId.ToString();
                if (httpContext != null)
                {
                    return httpContext.TraceIdentifier;
                }
                return Guid.NewGuid().ToString("N");
            }
        }

        public string CorrelationId
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    var newId = Guid.NewGuid().ToString("N");
                    _logger.LogWarning("No HttpContext available. Generating new CorrelationId: {CorrelationId}", newId);
                    return newId;
                }
                // Check if CorrelationId is already stored in HttpContext.Items
                if (httpContext.Items.TryGetValue(ContextItemName, out var cachedId) && cachedId is string sCachedId)
                {
                    return sCachedId;
                }
                // Try to get CorrelationId from request headers
                if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerId) && !string.IsNullOrWhiteSpace(headerId.FirstOrDefault()))
                {
                    var idFromHeader = headerId.First();
                    httpContext.Items[ContextItemName] = idFromHeader; // Cache lại
                    AddHeaderToResponse(httpContext, idFromHeader); // Đảm bảo ID này cũng có trong response
                    return idFromHeader;
                }
                // Generate a new CorrelationId if not found
                var newCorrelationId = Guid.NewGuid().ToString("N");
                _logger.LogWarning("X-Correlation-Id was missing. Generated new one: {CorrelationId}", newCorrelationId);
                httpContext.Items[ContextItemName] = newCorrelationId;
                AddHeaderToResponse(httpContext, newCorrelationId);
                return newCorrelationId;
            }
        }

        private void AddHeaderToResponse(HttpContext context, string id)
        {
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers[CorrelationIdHeaderName] = id;
                }
                return Task.CompletedTask;
            });

        }
    }
}
