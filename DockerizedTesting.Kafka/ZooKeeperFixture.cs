using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace DockerizedTesting.Kafka
{
    public class ZooKeeperFixture:BaseFixture<ZooKeeperOptions>, IFixtureWithCustomNetwork
    {
        public ZooKeeperFixture() : base("zookeeper", 1)
        {
        }

        public Task Start()
        {
            return this.Start(new ZooKeeperOptions());
        }

        public override Task Start(ZooKeeperOptions options)
        {
            this.Options = options;
            return base.Start(options);
        }


        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int zooKeeperPort = 2181;
            var hostConfig = Utils.HostWithBoundPorts(ports, zooKeeperPort);
            hostConfig.NetworkMode = this.NetworkName;

            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = new Dictionary<string, EmptyStruct>()
                    {
                        {zooKeeperPort.ToString(), default}
                    },
                    Env = new List<string>
                    {
                        "ZOO_ENABLE_AUTH=no",
                        "ALLOW_ANONYMOUS_LOGIN=yes"
                    },
                })
            {
                HostConfig = hostConfig
            };
        }
        protected override Task<bool> IsContainerRunning(int[] ports)
        {
            return Task.FromResult(true);
        }

        public string NetworkName { get; set; }
    }
}
