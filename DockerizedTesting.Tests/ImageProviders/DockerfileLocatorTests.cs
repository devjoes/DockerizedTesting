using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DockerizedTesting.ImageProviders;
using Xunit;

namespace DockerizedTesting.Tests.ImageProviders
{
    public class DockerfileLocatorTests
    {
        [Fact]
        public void ThrowsIfNoDockerFilesFound()
        {
            var locator = new DockerfileLocator();

            Assert.Throws<ArgumentException>(() => locator.GetDockerfile(new KeyValuePair<string, string>("this_wont_match", Guid.NewGuid().ToString())));
        }

        [InlineData("project", "foo")]
        [InlineData("label.with.equals", "foo=bar")]
        [InlineData("foo", "bar")]
        [InlineData("bar", "baz")]
        [Theory]
        public void ReturnsPathIfDockerFileFound(string key, string value)
        {
            var locator = new DockerfileLocator();
            var dockerFile = locator.GetDockerfile(new KeyValuePair<string, string>(key,value));
            Assert.NotNull(dockerFile);
            Assert.True(dockerFile.Exists);
            Assert.Equal("Dockerfile", dockerFile.Name);
        }
    }
}
