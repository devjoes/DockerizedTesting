using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using DockerizedTesting.Containers;
using DockerizedTesting.ImageProviders;
using Moq;
using Xunit;

namespace DockerizedTesting.Tests.ImageProviders
{
    public class DockerfileImageProviderTests
    {
        [Fact]
        public void ThrowsWhenDockerfilePathIsInvalid()
        {
            var file = new FileInfo(Path.GetTempFileName());
            file.Delete();
            Assert.Throws<FileNotFoundException>(() => new DockerfileImageProvider(file, "."));
        }

        [Fact]
        public void ThrowsWhenDockerContextPathIsInvalid()
        {
            var file = new FileInfo(Path.GetTempFileName());
            try
            {
                Assert.Throws<FileNotFoundException>(() => new DockerfileImageProvider(file,
                    Path.Combine("..", Guid.NewGuid().ToString())));
            }
            finally
            {
                file.Delete();
            }
        }


        [Theory]
        [InlineData("FROM busybox\n\nRUN kabooom!!!! build error!!\n")]
        [InlineData("FROM busybox\n\nRUN \"echo syntactically this is not how you do docker images\n")]
        public async Task ThrowsWhenDockerBuildFails(string content)
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            var file = new FileInfo(Path.Combine(dir, "Dockerfile"));
            File.WriteAllText(file.FullName,content);
            try
            {
                var client = new DockerClientProvider().GetDockerClient();
                var provider = new DockerfileImageProvider(file, ".");
                await Assert.ThrowsAsync<DockerBuildFailedException>(async () => await provider.GetImage(client));
            }
            finally
            {
                Directory.Delete(dir,true);
            }
        }
        
        [Fact]
        public async Task BuildsImageFromDockerfile()
        {
            var dockerFile = ProjectLocator.FindSlnFiles().First().Directory
                .GetDirectories("Examples").Single()
                .GetDirectories("ExampleProject").Single()
                .GetFiles("Dockerfile").Single();

            var client = new DockerClientProvider().GetDockerClient();
            var provider = new DockerfileImageProvider(dockerFile, "..");

            string image = await provider.GetImage(client);

            Assert.NotNull(image);
        }

        [Fact]
        public async Task FindsDockerFileWhenSuppliedWithProjectName()
        {
            var dockerFile = ProjectLocator.FindSlnFiles().First().Directory
                .GetDirectories("Examples").Single()
                .GetDirectories("ExampleProject").Single()
                .GetFiles("Dockerfile").Single();

            var locator = new Mock<IDockerfileLocator>();
            locator.Setup(l => l.GetDockerfile(new KeyValuePair<string, string>("project", "foo")))
                .Returns(dockerFile).Verifiable();
            var client = new DockerClientProvider().GetDockerClient();
            var provider = new DockerfileImageProvider("foo", "..", null, locator.Object);
            var image = await provider.GetImage(client);
            Assert.NotNull(image);
            locator.Verify();
        }
    }
}
