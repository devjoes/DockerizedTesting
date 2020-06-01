using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DockerizedTesting.MsSql.Tests
{
    public class MsSqlFixtureTests:IClassFixture<MsSqlFixture>
    {
        private readonly MsSqlFixture fixture;

        public MsSqlFixtureTests(MsSqlFixture fixture)
        {
            fixture.Start().Wait();
            this.fixture = fixture;
        }

        [Fact]
        public async Task MsSqlIsReachable()
        {
            Assert.True(this.fixture.ContainerStarting);
            Assert.True(this.fixture.ContainerStarted);

            var endpoint = this.fixture.Endpoints.Single();
            var connection = new SqlConnection(
                    $"Server={endpoint.Hostname},{endpoint.Port};Database=master;TrustServerCertificate=True;User Id=sa;Password={this.fixture.Options.SaPassword}");

            await connection.OpenAsync();
            var cmd = new SqlCommand("SELECT 1+1", connection);
            var result = await cmd.ExecuteScalarAsync();

            Assert.Equal(2,result);
        }
    }
}
