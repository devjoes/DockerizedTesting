using System;
using System.IO;
using System.Linq;
using DockerizedTesting.ImageProviders;
using Xunit;

namespace DockerizedTesting.Tests.ImageProviders
{
    public class ProjectLocatorTests
    {
        [Fact]
        public void FindSlnFiles_ReturnsSlnFiles()
        {
            var files = ProjectLocator.FindSlnFiles().ToArray();
            Assert.NotEmpty(files);
            Assert.NotNull(files.FirstOrDefault(f => f.Name == $"{nameof(DockerizedTesting)}.sln"));
        }

        [Fact]
        public void GetProjectsInSln_ReturnsDockerizedProjects()
        {
            var slnPath = ProjectLocator.GetCurrentDir().Parent?.Parent?.Parent?.Parent?.GetFiles("*.sln").SingleOrDefault()?.FullName;
            if (slnPath == null)
            {
                throw new FileNotFoundException("Can't find sln file");
            }
            var locator = new ProjectLocator();
            var dockerizedProjects = locator.GetProjectNamesFromSln(slnPath).ToArray();
            var exampleProj = dockerizedProjects.SingleOrDefault(p => p.names.Contains("ExampleProjectNs"));
            Assert.True(File.Exists(exampleProj.path));
            Assert.Contains("ExampleProject", exampleProj.names);
            Assert.Contains("ExampleProjectAsm", exampleProj.names);
            Assert.Contains("ExampleProjectNs", exampleProj.names);

            var allProjects = locator.GetProjectNamesFromSln(slnPath, _ => true).ToArray();
            Assert.True(allProjects.Length > dockerizedProjects.Length);
        }

        [Fact]
        public void GetPathOfDockerProjectReturnsNullWhenNotFound()
        {
            var projectLocator = new ProjectLocator();
            Assert.Null(projectLocator.GetDockerProject(Guid.NewGuid().ToString()));
        }


        [Theory]
        [InlineData("ExampleProject")]
        [InlineData("ExampleProjectNs")]
        [InlineData("ExampleProjectAsm")]
        void GetPathOfDockerProjectReturnsCorrectPath(string name)
        {
            var projectLocator = new ProjectLocator();
            var fileInfo = projectLocator.GetDockerProject(name);

            Assert.True(fileInfo.Exists);
            Assert.Equal("ExampleProject.csproj", fileInfo.Name);
        }
    }
}
