using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DockerizedTesting.Containers;
using MongoDB.Driver;
using Xunit;

namespace DockerizedTesting.Mongo.Tests
{

    public class MongoFixtureTests : IClassFixture<MongoFixture>
    {
        private readonly MongoFixture mongoFixture;

        public MongoFixtureTests(MongoFixture mongoFixture)
        {
            mongoFixture.Start().Wait();
            this.mongoFixture = mongoFixture;
        }

        [Fact]
        public async Task MongoIsReachable()
        {
            Assert.True(this.mongoFixture.ContainerStarting);
            Assert.True(this.mongoFixture.ContainerStarted);

            await this.hitMongo(this.mongoFixture);
        }

        [Fact]
        public async Task DisposeKillsMongo()
        {
            var fixture = new MongoFixture();
            await fixture.Start();
            Assert.True(fixture.ContainerStarting);
            Assert.True(fixture.ContainerStarted);

            fixture.Dispose();

            var ex = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                try
                {
                    await new MongoClient("mongodb://localhost:" + fixture.Ports.Single())
                        .ListDatabaseNamesAsync();
                }
                catch (Exception e)
                {
                    throw new AggregateException(e);
                }
            });
            Assert.True(ex.InnerException is MongoConnectionException || ex.InnerException is TimeoutException);
        }

        //[Fact]
        //public async Task ContainersAreRemovedOnShutdown()
        //{
        //    int rnd = new Random().Next(10000,12000-1);
        //    async Task<string> StartStopContainer()
        //    {
        //        var fixture = new MongoFixture();
        //        var options = new MongoFixtureOptionsWithOwnHost();
        //        // This ensures that the container will be unique and not being used by a diff test.
        //        fixture.Ports[0] = rnd;
        //        await fixture.Start(options);
        //        Assert.True(fixture.ContainerStarting);
        //        Assert.True(fixture.ContainerStarted);
        //        string id = fixture.ContainerId;
        //        await Task.Delay(1000);
        //        fixture.Dispose();
        //        await Task.Delay(1000);
        //        options.ContainerHost.Dispose();
        //        await Task.Delay(1000);
        //        return id;
        //    }

        //    var id1 = await StartStopContainer();
        //    await Task.Delay(5000);
        //    var id2 = await StartStopContainer();

        //    Assert.NotEqual(id1,id2);
        //}

        [Fact]
        public async Task ThreadSafetyTest() =>
            Assert.Null(await Record.ExceptionAsync(() =>
            Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
                    Task.Run(async () => (await this.hitMongo(new MongoFixture())).Dispose())))));


        private async Task<MongoFixture> hitMongo(MongoFixture fixture)
        {
            await fixture.Start();
            var db = new MongoClient("mongodb://localhost:" + fixture.Ports.Single())
                .GetDatabase(Guid.NewGuid().ToString());
            await db.CreateCollectionAsync("test");
            await db.GetCollection<Foo>("test")
                .InsertManyAsync(Enumerable.Range(0, 2000).Select(i => new Foo { Bar = i }));
            return fixture;
        }

        public class Foo
        {
            public int Bar { get; set; }
        }

    }

}
