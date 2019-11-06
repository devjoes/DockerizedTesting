using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.Mongo
{
    public class MongoFixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerHubImageProvider("mongo:latest");
    }
}