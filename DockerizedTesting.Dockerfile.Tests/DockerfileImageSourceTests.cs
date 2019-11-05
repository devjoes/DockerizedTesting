using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Moq;
using Xunit;

namespace DockerizedTesting.Dockerfile.Tests
{
    public class DockerfileImageSourceTests
    {
        [Fact]
        public async Task GetImageThrowsWhenProjectCantBeFound()
        {
            var file = new FileInfo(Path.GetTempFileName());
            file.Delete();
            var imageSource = new DockerfileImageProvider(file);
            await Assert.ThrowsAsync<FileNotFoundException>(async () => await imageSource.GetImage(null));
        }

        [Fact]
        public async Task GetImageThrowsWhenProjectIsNull()
        {
            var imageSource = new DockerfileImageProvider((string)null);
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await imageSource.GetImage(null));
        }

        [Fact]
        public async Task GetImageThrowsWhenProjectFailsToBuild()
        {
            var file = new FileInfo(Path.GetTempFileName());
            try
            {
                var imageSource = new DockerfileImageProvider(file);
                await Assert.ThrowsAsync<DockerProjectBuildFailedException>(async () => await imageSource.GetImage(null));
            }
            finally
            {
                file.Delete();
            }
        }


        [Fact]
        public async Task GetImageBuildsProjectFromFile()
        {
            var file = ProjectLocator.GetCurrentDir().Parent?.Parent?.Parent?.Parent
                ?.GetFiles("ExampleProject.csproj", SearchOption.AllDirectories).Single();
            var imageSource = new DockerfileImageProvider(file);

            string tag = await imageSource.GetImage(null);
            var image = await new DockerClientProvider().GetDockerClient().Images
                .ListImagesAsync(new ImagesListParameters { MatchName = tag });

            Assert.Single(image);
        }

        [Fact]
        public async Task GetImageBuildsProjectFromNames()
        {
            string name = "foo";
            var file = ProjectLocator.GetCurrentDir().Parent?.Parent?.Parent?.Parent
                ?.GetFiles("ExampleProject.csproj", SearchOption.AllDirectories).Single();
            var projectLocator = new Mock<IProjectLocator>();
            
            projectLocator.Setup(p => p.GetDockerProject(name)).Returns(file);

            var imageSource = new DockerfileImageProvider(name, projectLocator.Object);

            string tag = await imageSource.GetImage(null);
            var image = await new DockerClientProvider().GetDockerClient().Images
                .ListImagesAsync(new ImagesListParameters { MatchName = tag });

            Assert.Single(image);
        }
    }
}
