using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace DockerizedTesting.Kafka
{
    public class KafkaFixture : ComposedFixture<ZooKeeperFixture, KafkaNodeFixture>
    {
        private const string NetworkName = "net-kafka-zookeeper";

        public KafkaFixture() : base(NetworkName)
        {
        }

        public async Task Start()
        {
            await this.Start(new KafkaFixtureOptions(), new ZooKeeperOptions());
        }

        public async Task Start(KafkaFixtureOptions kafkaFixtureOptions, ZooKeeperOptions zooKeeperOptions)
        {
            var setZookeeperName = new Func<ComposedFixture, KafkaFixtureOptions, KafkaFixtureOptions>(
                (c, o) =>
                {
                    o.ZooKeeper = c.Fixtures.Last(f => f is ZooKeeperFixture && !f.IsDisposed).UniqueContainerName;
                    return o;
                });
            await base.Start(
                (f, _) => f.Start(zooKeeperOptions),
                (f, c) => f.Start(setZookeeperName(c, kafkaFixtureOptions)));
            await this.WaitForContainers();
        }

        public KafkaNodeFixture Kafka
        {
            get
            {
                return this.Fixtures.LastOrDefault(f => f is KafkaNodeFixture && !f.IsDisposed) as KafkaNodeFixture ??
                    this.Fixtures.LastOrDefault(f => f is KafkaNodeFixture) as KafkaNodeFixture;
            }
        }

        public ZooKeeperFixture ZooKeeper
        {
            get
            {
                return this.Fixtures.LastOrDefault(f => f is ZooKeeperFixture && !f.IsDisposed) as ZooKeeperFixture ??
                       this.Fixtures.LastOrDefault(f => f is ZooKeeperFixture) as ZooKeeperFixture;
            }
        }


        public async Task WaitForContainers()
        {
            this.ContainerStarting = true;
            var delayMs = Math.Max(this.Kafka.Options.DelayMs, this.ZooKeeper.Options.DelayMs);
            var maxRetries = Math.Max(this.Kafka.Options.MaxRetries, this.ZooKeeper.Options.MaxRetries);
            int attempts = 0;
            do
            {
                this.ContainerStarted = await this.connectToKafka();
                await Task.Delay(delayMs);
            } while (!this.ContainerStarted && attempts++ <= maxRetries);
        }

        public bool ContainerStarted { get; set; }
        public bool ContainerStarting { get; set; }

        public int KafkaPort => this.Kafka.Ports.Single();
        public int ZooKeeperPort => this.ZooKeeper.Ports.Single();
        public string Ip => this.Kafka.Ip;

        private async Task<bool> connectToKafka()
        {
            try
            {
                //var log = new BlockingCollection<string>();
                int port = this.Kafka.Ports.Single();
                var config = new AdminClientConfig
                {
                    BootstrapServers = this.Ip + ":" + port,
                }; // + ports.First()};

                using (var client = new AdminClientBuilder(config).Build())
                //.SetLogHandler((adminClient, message) =>log.Add(message.Message)).Build()
                {
                    await this.createMissingTopics(client);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task createMissingTopics(IAdminClient client)
        {
            var metadata = client.GetMetadata(TimeSpan.FromSeconds(30));
            var missingTopics = this.Kafka.Options.Topics.Where(tOpt => metadata.Topics.TrueForAll(t => t.Topic != tOpt.Name));

            var sw = Stopwatch.StartNew();
            await client.CreateTopicsAsync(missingTopics.Select(t => new TopicSpecification
            {
                Name = t.Name,
                NumPartitions = t.Partitions,
                ReplicationFactor = t.ReplicationFactor
            }), new CreateTopicsOptions { OperationTimeout = TimeSpan.FromSeconds(30), RequestTimeout = TimeSpan.FromSeconds(30) });
            sw.Stop();
        }
    }
}
