using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DockerizedTesting.ImageProviders;
using DockerizedTesting.MsSql;
using Xunit;

namespace ExampleDatabase.Tests
{
    public class ReadDataFromDb : IClassFixture<MsSqlFixture>
    {
        private readonly MsSqlFixture fixture;

        public ReadDataFromDb(MsSqlFixture fixture)
        {
            fixture.Start(new CustomMsSqlFixtureOptions()).Wait();
            this.fixture = fixture;
        }

        [Fact]
        public async Task ReadValue()
        {
            using (SqlConnection con = new SqlConnection(this.fixture.GetMsSqlConnectionString(this.fixture.Ports.Single(), "test")))
            {
                await con.OpenAsync();
                using (var cmd = new SqlCommand("SELECT Foo FROM Test WHERE Id = 1", con))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    Assert.Equal("Bar", result);
                }
            }
        }
    }

    public class CustomMsSqlFixtureOptions : MsSqlFixtureOptions
    {
        public CustomMsSqlFixtureOptions()
        {
            ImageProvider = new DockerfileImageProvider("example_database", ".",
                new ImageBuildParameters
                {
                    BuildArgs = new Dictionary<string, string>()
                    {
                        {"PASSWORD", this.SaPassword},
                        {"DATABASE","test" }
                    }
                });
        }
        public override IDockerImageProvider ImageProvider { get; }
    }
}
