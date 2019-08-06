namespace DockerizedTesting.Kafka
{
    public class KafkaTopic
    {
        public string Name { get; set; }
        public int Partitions { get; set; } = 1;
        public short ReplicationFactor { get; set; } = 1;
    }
}