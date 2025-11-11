namespace DeliveryService.Api
{
    public class KafkaOptions
    {
        public const string Kafka = "Kafka";
        public string BootstrapServers { get; set; } = string.Empty;
        public string GroupId { get; set; } = "delivery-service-group";
    }
}
