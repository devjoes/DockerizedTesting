using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DockerizedTesting;
using DockerizedTesting.ImageProviders;
using Xunit;

namespace ExampleProject.Tests
{
    public class ExampleProjectTests : IClassFixture<ExampleTestFixture>
    {
        private readonly ExampleTestFixture fixture;

        public ExampleProjectTests(ExampleTestFixture fixture)
        {
            this.fixture = fixture;
            this.fixture.Start(new ExampleTestFixtureOptions()).Wait();
        }

        [Fact]
        public async Task TestServer()
        {
            var client = new HttpClient();
            string response = await client.GetStringAsync("http://localhost:" + this.fixture.Ports.Single());
            Assert.Equal("Hello world!", response);
        }
    }

    public class ExampleTestFixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerProjectImageProvider(nameof(ExampleProjectNs));
    }

    public class ExampleTestFixture : BaseFixture<ExampleTestFixtureOptions>
    {
        public ExampleTestFixture() : base("example", 1)
        {
        }

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int port = 80;
            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = new Dictionary<string, EmptyStruct>() { { port.ToString(), default } }
                })
            {
                HostConfig = Utils.HostWithBoundPorts(ports, port)
            };
        }

        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            var client = new HttpClient();
            try
            {
                await client.GetStringAsync("http://localhost:" + ports.Single());
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}
