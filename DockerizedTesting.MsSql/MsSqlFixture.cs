using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;

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

        public string GetMsSqlConnectionString(int port, string database = "master") => $"Server=localhost,{port};Database={database};TrustServerCertificate=True;User Id=sa;Password={this.Options.SaPassword};Connect Timeout=5";

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

        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            try
            {
                var connectionString = this.GetMsSqlConnectionString(ports.Single());
                var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                var cmd = new SqlCommand("SELECT GETDATE()", connection);
                await cmd.ExecuteScalarAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}

