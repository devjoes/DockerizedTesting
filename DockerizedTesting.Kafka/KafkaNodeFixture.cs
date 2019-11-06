using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Docker.DotNet.Models;
using Config = Docker.DotNet.Models.Config;

namespace DockerizedTesting.Kafka
{
    public class KafkaNodeFixture : BaseFixture<KafkaFixtureOptions>,IFixtureWithCustomNetwork
    {
        public KafkaNodeFixture() : base("kafka", 1)
        {
        }

        private string getIp()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .First(i => i.AddressFamily == AddressFamily.InterNetwork).ToString();
            }
        }

        public Task Start()
        {
            return this.Start(new KafkaFixtureOptions());
        }

        public override Task Start(KafkaFixtureOptions options)
        {
            this.Options = options;
            this.Ip = this.Options.IpAddress ?? this.getIp();
            return base.Start(options);
        }

        public string Ip { get; set; }

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int kafkaPort = 9092;

            var hostConfig = Utils.HostWithBoundPorts(ports, kafkaPort);
            hostConfig.NetworkMode = this.NetworkName;

            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = new Dictionary<string, EmptyStruct>()
                    {
                        {kafkaPort.ToString(), default}
                    },
                    Env = new List<string>
                    {
                        $"KAFKA_CFG_ZOOKEEPER_CONNECT={this.Options.ZooKeeper}:2181",
                        "ALLOW_PLAINTEXT_LISTENER=yes",
                        "KAFKA_CFG_LISTENERS=PLAINTEXT://0.0.0.0:"+kafkaPort,
                        "KAFKA_CFG_ADVERTISED_LISTENERS=PLAINTEXT://"+ this.Ip+":"+ports.Single(),
                    }
                })
            {
                HostConfig = hostConfig
            };
        }

        protected override  Task<bool> IsContainerRunning(int[] ports)
        {
            return Task.FromResult(true);
        }

        public string NetworkName { get; set; }
    }
}
