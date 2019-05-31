using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DockerizedTesting;
using StackExchange.Redis;

namespace RedisTesting
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
            this.dockerImage = options.Image;
            return base.Start(options);
        }

        private string dockerImage;

        protected string GetRedisConfiguration(int port) => $"localhost:{port}";

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int redisPort = 6379;
            return new CreateContainerParameters(
                new Config
                {
                    Image = this.dockerImage,
                    ExposedPorts = new Dictionary<string, EmptyStruct>() { { redisPort.ToString(), default } }
                })
            {
                HostConfig = Utils.HostWithBoundPorts(ports, redisPort)
            };
        }

        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            try
            {
                var configuration = this.GetRedisConfiguration(ports.Single());
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
