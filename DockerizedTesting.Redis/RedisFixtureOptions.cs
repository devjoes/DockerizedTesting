namespace DockerizedTesting.Redis
{
    public class RedisFixtureOptions : FixtureOptions
    {
        public string Image { get; set; } = "redis:latest";
    }
}