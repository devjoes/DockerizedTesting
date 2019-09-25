using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using MongoDB.Driver;

namespace DockerizedTesting.Mongo
{
    public class MongoFixture : BaseFixture<MongoFixtureOptions>
    {
        public MongoFixture() : base("mongo", 1)
        {
        }

        public Task Start()
        {
            return this.Start(new MongoFixtureOptions());
        }

        public override Task Start(MongoFixtureOptions options)
        {
            this.dockerImage = options.Image;
            return base.Start(options);
        }

        private string dockerImage;

        protected string GetMongoConnectionString(int port) => $"mongodb://localhost:{port}?connectTimeoutMS=2000";

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int mongoPort = 27017;
            return new CreateContainerParameters(
                new Config
                {
                    Image = this.dockerImage,
                    ExposedPorts = new Dictionary<string, EmptyStruct>() { { mongoPort.ToString(), default } },
                    Env = new List<string>(new[]
                    {
                        "MONGO_DATA_DIR=/data/db",
                        "MONGO_LOG_DIR=/dev/null"
                    }),
                })
            {
                HostConfig = Utils.HostWithBoundPorts(ports, mongoPort)
            };
        }

        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            try
            {
                var connectionString = this.GetMongoConnectionString(ports.Single());
                var client = new MongoClient(connectionString);
                await (await client.ListDatabaseNamesAsync()).ToListAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
