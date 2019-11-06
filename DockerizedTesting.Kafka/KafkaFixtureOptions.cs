using System;
using System.Collections.Generic;
using System.Text;
using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.Kafka
{
    public class KafkaFixtureOptions:FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerHubImageProvider("bitnami/kafka:latest");
        public List<KafkaTopic> Topics { get; set; } = new List<KafkaTopic>();
        public string ZooKeeper { get; set; }
        public string IpAddress { get; set; }
    }
}
