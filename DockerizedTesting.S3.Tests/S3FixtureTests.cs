using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using DockerizedTesting.Containers;
using Xunit;

namespace DockerizedTesting.S3.Tests
{
    public class S3FixtureTests : IClassFixture<S3Fixture>
    {
        private readonly S3Fixture s3Fixture;

        public S3FixtureTests(S3Fixture s3Fixture)
        {
            s3Fixture.Start().Wait();
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

            await this.hitS3(this.s3Fixture);

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

        //[Fact]
        //public async Task ContainersAreRemovedOnShutdown()
        //{
        //    int rnd = new Random().Next(10000, 12000 - 1);

        //    async Task<string> StartStopContainer()
        //    {
        //        var fixture = new S3Fixture();
        //        var options = new S3FixtureOptionsWithOwnHost();
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

        //    Assert.NotEqual(id1, id2);
        //}

        private async Task<S3Fixture> hitS3(S3Fixture fixture)
        {
            await fixture.Start();

            var s3Client = new AmazonS3Client(
                new AnonymousAWSCredentials(),
                new AmazonS3Config
                {
                    ServiceURL = "http://127.0.0.1:" + fixture.Ports.Single(),
                    ForcePathStyle = true,
                    Timeout = TimeSpan.FromSeconds(5)
                });
            var cts = new CancellationTokenSource();
            cts.CancelAfter(6000);
            const string bucketName = "bar";
            await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }, cts.Token);
            await s3Client.ListBucketsAsync(cts.Token);
            await s3Client.DeleteBucketAsync(bucketName, cts.Token);
            return fixture;
        }

    }
}