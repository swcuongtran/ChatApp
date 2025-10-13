namespace Contracts.API
{
    public sealed record ErrorEnvelope
    (
        string Code,
        string Message,
        string TraceId,
        string? CorrelationId = null,
        object? Details = null
    );
}
