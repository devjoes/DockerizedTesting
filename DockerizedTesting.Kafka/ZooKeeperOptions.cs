namespace DockerizedTesting.Kafka
{
    public class ZooKeeperOptions : FixtureOptions
    {
        public string Image { get; set; } = "bitnami/zookeeper:latest";
    }
}