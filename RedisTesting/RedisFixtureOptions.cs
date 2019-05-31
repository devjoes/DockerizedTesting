using DockerizedTesting;

namespace RedisTesting
{
    public class RedisFixtureOptions : FixtureOptions
    {
        public string Image { get; set; } = "redis:latest";
    }
}