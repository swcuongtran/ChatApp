namespace Utils.Generator
{
    public interface IIdGenerator
    {
        string NewId();
    }
    public sealed class GuidIdGenerator : IIdGenerator
    {
        public string NewId() => Guid.NewGuid().ToString("N");
    }
}
