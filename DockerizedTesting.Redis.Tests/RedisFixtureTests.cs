using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

        [Fact]
        public async Task ContainersAreRemovedOnShutdown()
        {
            int rnd = new Random().Next(10000, 12000 - 1);
            async Task<string> StartStopContainer()
            {
                var fixture = new RedisFixture();
                var options = new RedisFixtureOptionsWithOwnHost();
                options.ContainerHost.RemoveContainersOnExit = true;
                // This ensures that the container will be unique and not being used by a diff test.
                fixture.Ports[0] = rnd;
                await fixture.Start(options);
                Assert.True(fixture.ContainerStarting);
                Assert.True(fixture.ContainerStarted);
                string id = fixture.ContainerId;
                await Task.Delay(1000);
                fixture.Dispose();
                await Task.Delay(1000);
                options.ContainerHost.Dispose();
                await Task.Delay(1000);
                return id;
            }

            var id1 = await StartStopContainer();
            await Task.Delay(5000);
            var id2 = await StartStopContainer();

            Assert.NotEqual(id1, id2);
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

        public class RedisFixtureOptionsWithOwnHost : RedisFixtureOptions
        {
            public RedisFixtureOptionsWithOwnHost()
            {
                var ctor = typeof(ContainerHost).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new Type[0],
                    null);
                this.ContainerHost = (IContainerHost)ctor.Invoke(null);
            }
            public override IContainerHost ContainerHost { get; }
        }
    }
}
