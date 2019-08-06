using System;
using System.Collections.Generic;
using System.Text;

namespace DockerizedTesting.Kafka
{
    public class KafkaFixtureOptions:FixtureOptions
    {
        public string Image { get; set; } = "bitnami/kafka:latest";
        public List<KafkaTopic> Topics { get; set; } = new List<KafkaTopic>();
        public string ZooKeeper { get; set; }
        public string IpAddress { get; set; }
    }
}
