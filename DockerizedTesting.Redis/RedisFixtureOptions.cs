using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.Redis
{
    public class RedisFixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerHubImageProvider("redis:latest");
    }
}