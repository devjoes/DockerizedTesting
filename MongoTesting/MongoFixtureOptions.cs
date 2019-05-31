using DockerizedTesting;

namespace MongoTesting
{
    public class MongoFixtureOptions : FixtureOptions
    {
        public string Image { get; set; } = "mongo:latest";
    }
}