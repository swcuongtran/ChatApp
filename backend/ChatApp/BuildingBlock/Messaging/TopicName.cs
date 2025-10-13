namespace BuildingBlock.Messaging
{
    public static class TopicName
    {
        public static string Build(string domain, string entity, string @event, int version) => $"{domain}.{entity}.{@event}.v{version}";
    }
}
