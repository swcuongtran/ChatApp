namespace ChatApp.Gateway.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext ctx)
        {
            if (!ctx.Request.Headers.TryGetValue(HeaderName, out var corr) || string.IsNullOrWhiteSpace(corr))
            {
                corr = Guid.NewGuid().ToString("N");
                ctx.Request.Headers[HeaderName] = corr;
            }
            ctx.Response.OnStarting(() =>
            {
                ctx.Response.Headers[HeaderName] = corr!;
                return Task.CompletedTask;
            });
            await _next(ctx);
        }
    }
}
