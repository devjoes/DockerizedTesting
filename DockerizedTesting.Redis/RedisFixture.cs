using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DockerizedTesting.Models;
using StackExchange.Redis;

namespace DockerizedTesting.Redis
{
    public class RedisFixture: BaseFixture<RedisFixtureOptions>
    {
        public RedisFixture() : base("redis", 1)
        {
        }

        public Task Start()
        {
            return this.Start(new RedisFixtureOptions());
        }

        public override Task Start(RedisFixtureOptions options)
        {
            return base.Start(options);
        }

        protected string GetRedisConfiguration(HostEndpoint endpoint) => 
            $"{endpoint.Hostname}:{endpoint.Port},connectTimeout=3000,connectRetry=1";

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int redisPort = 6379;
            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = new Dictionary<string, EmptyStruct>() { { redisPort.ToString(), default } }
                })
            {
                HostConfig = Utils.HostWithBoundPorts(ports, redisPort)
            };
        }

        protected override async Task<bool> IsContainerRunning(HostEndpoint[] endpoints)
        {
            try
            {
                var configuration = this.GetRedisConfiguration(endpoints.Single());
                var redis = await ConnectionMultiplexer.ConnectAsync(configuration);

                await redis.GetDatabase().HashKeysAsync("test");
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
