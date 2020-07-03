using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Xunit;
using Xunit.Abstractions;

namespace DockerizedTesting.Azurite.Tests
{
    public class AzuriteFixtureTests : IClassFixture<AzuriteFixture>
    {
        private readonly AzuriteFixture fixture;

        public AzuriteFixtureTests(AzuriteFixture fixture, ITestOutputHelper output)
        {
            var workspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(workspace);
            fixture.Start(new AzuriteFixtureOptions
            {
                StorageAccountKey = "AAA8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==",
                StorageAccountName = "foobar",
                DebugLog = Path.GetTempFileName(),
                Workspace = workspace
            }).GetAwaiter().GetResult();
            this.fixture = fixture;
        }

        [Fact]
        public async Task AzuriteIsReachable()
        {
            string content = Guid.NewGuid().ToString();
            string container = "foo";
            var client = new BlobContainerClient(this.fixture.BlobConnectionString,
                $"{this.fixture.Options.StorageAccountName}/{container}");
            await client.CreateIfNotExistsAsync();
            string blobName = "bar";
            await client.UploadBlobAsync(blobName, new MemoryStream(Encoding.UTF8.GetBytes(content)));
            var files = Directory.GetFiles(Path.Combine(this.fixture.Options.Workspace, "__blobstorage__"));
            Assert.Single(files);
            this.fixture.Dispose();
            Assert.Equal(content, await File.ReadAllTextAsync(files.Single()));

        }
    }
}
