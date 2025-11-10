namespace Utils.Correlation
{
    public interface ICorrelationIdProvider
    {
        string TraceId { get; }
        string CorrelationId { get; }
    }
}
