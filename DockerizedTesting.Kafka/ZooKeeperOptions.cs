using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.Kafka
{
    public class ZooKeeperOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerRepoImageProvider("bitnami/zookeeper:latest");
    }
}