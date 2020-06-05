using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DockerizedTesting.Models;

namespace DockerizedTesting.MsSql
{
    public class MsSqlFixture : BaseFixture<MsSqlFixtureOptions>
    {
        public MsSqlFixture() : base("mssql", 1)
        {
        }

        public Task Start()
        {
            return this.Start(new MsSqlFixtureOptions());
        }

        public override Task Start(MsSqlFixtureOptions options)
        {
            return base.Start(options);
        }

        public string GetMsSqlConnectionString(HostEndpoint endpoint, string database = "master") => 
            $"Server={endpoint.Hostname},{endpoint.Port};Database={database};TrustServerCertificate=True;User Id=sa;Password={this.Options.SaPassword};Connect Timeout=5";

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int port = 1433;
            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = new Dictionary<string, EmptyStruct>() { { port.ToString(), default } },
                    Env = new List<string>(new[]
                    {
                        "ACCEPT_EULA="+(this.Options.AcceptEula ? "Y":"N"),
                        "SA_PASSWORD="+this.Options.SaPassword,
                        "MSSQL_PID="+this.Options.Product
                    }),
                })
            {
                HostConfig = Utils.HostWithBoundPorts(ports, port)
            };
        }

        protected override async Task<bool> IsContainerRunning(HostEndpoint[] endpoints, CancellationToken cancellationToken)
        {
            try
            {
                var connectionString = this.GetMsSqlConnectionString(endpoints.Single());
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                var cmd = new SqlCommand("SELECT GETDATE()", connection);
                await cmd.ExecuteScalarAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}

