namespace DockerizedTesting.Mongo
{
    public class MongoFixtureOptions : FixtureOptions
    {
        public string Image { get; set; } = "mongo:latest";
    }
}