using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace DockerizedTesting.S3.Tests
{
    public class S3FixtureTests : IClassFixture<S3Fixture>, IDisposable
    {
        private readonly S3Fixture s3Fixture;
        private string tmpFile;

        public S3FixtureTests(S3Fixture s3Fixture)
        {
            this.tmpFile = Path.GetTempFileName();
            s3Fixture.Start(new S3FixtureOptions { VolumePath = Path.GetTempPath() }).Wait();
            this.s3Fixture = s3Fixture;
        }

        [Fact]
        public async Task S3IsReachable()
        {
            Assert.True(this.s3Fixture.ContainerStarting);
            Assert.True(this.s3Fixture.ContainerStarted);

            await this.hitS3(this.s3Fixture);
        }


        [Fact]
        public async Task S3FileIsReachable()
        {
            Assert.True(this.s3Fixture.ContainerStarting);
            Assert.True(this.s3Fixture.ContainerStarted);
            
            await this.hitS3(this.s3Fixture, new FileInfo(this.tmpFile).Name);

            
        }


        [Fact]
        public async Task S3USesTempDirIfNotProvided()
        {
            using (var tmpFixture = new S3Fixture())
            {
                tmpFixture.Start(new S3FixtureOptions {VolumePath = Path.GetTempPath()}).Wait();
                await this.hitS3(tmpFixture);
            }
        }

        [Fact]
        public async Task DisposeKillsS3()
        {
            var fixture = new S3Fixture();
            await fixture.Start();
            Assert.True(fixture.ContainerStarting);
            Assert.True(fixture.ContainerStarted);

            fixture.Dispose();

            using (var client = new HttpClient())
            {
                await Assert.ThrowsAsync<HttpRequestException>(async () =>
                {
                    var result = await client.GetAsync("http://localhost:" + fixture.Ports.Single());
                    result.EnsureSuccessStatusCode();
                });
            }
        }

        [Fact]
        public async Task ContainersAreRemovedOnShutdown()
        {
            int rnd = new Random().Next(10000, 12000 - 1);

            async Task<string> StartStopContainer()
            {
                var fixture = new S3Fixture();
                var options = new S3FixtureOptionsWithOwnHost();
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

        private async Task<S3Fixture> hitS3(S3Fixture fixture, string keyPrefix = "")
        {
            await fixture.Start();

            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"http://localhost:{fixture.Ports.Single()}/{keyPrefix}");
                result.EnsureSuccessStatusCode();
            }

            return fixture;
        }

        public class S3FixtureOptionsWithOwnHost : S3FixtureOptions
        {
            public S3FixtureOptionsWithOwnHost()
            {
                var ctor = typeof(ContainerHost).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                    null, new Type[0],
                    null);
                this.ContainerHost = (IContainerHost)ctor.Invoke(null);
            }

            public override IContainerHost ContainerHost { get; }
        }

        public void Dispose()
        {
            File.Delete(this.tmpFile);
        }
    }
}