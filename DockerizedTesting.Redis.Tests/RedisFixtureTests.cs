using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DockerizedTesting.Containers;
using StackExchange.Redis;
using Xunit;

namespace DockerizedTesting.Redis.Tests
{
    public class RedisFixtureTests : IClassFixture<RedisFixture>
    {
        private readonly RedisFixture redisFixture;

        public RedisFixtureTests(RedisFixture redisFixture)
        {
            redisFixture.Start().Wait();
            this.redisFixture = redisFixture;
        }

        [Fact]
        public async Task RedisIsReachable()
        {
            Assert.True(this.redisFixture.ContainerStarting);
            Assert.True(this.redisFixture.ContainerStarted);

            await this.hitRedis(this.redisFixture);
        }

        [Fact]
        public async Task DisposeKillsRedis()
        {
            var fixture = new RedisFixture();
            await fixture.Start();
            Assert.True(fixture.ContainerStarting);
            Assert.True(fixture.ContainerStarted);

            fixture.Dispose();

            await Assert.ThrowsAsync<RedisConnectionException>(async () =>
                (await ConnectionMultiplexer.ConnectAsync("localhost:" + fixture.Ports.Single())).GetDatabase()
                .SetAdd("foo", "bar"));
        }

        private async Task<RedisFixture> hitRedis(RedisFixture fixture)
        {
            await fixture.Start();
            var db = (await ConnectionMultiplexer.ConnectAsync("localhost:" + fixture.Ports.Single())).GetDatabase();
            db.SetAdd("foo", "bar");
            return fixture;
        }

        public class Foo
        {
            public int Bar { get; set; }
        }

    }
}
