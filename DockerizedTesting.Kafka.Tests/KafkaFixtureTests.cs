using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using DockerizedTesting.Models;
using DockerizedTesting.Tests.Containers;
using Xunit;
using Xunit.Abstractions;

namespace DockerizedTesting.Kafka.Tests
{
    public class KafkaFixtureTests : IClassFixture<KafkaFixture>
    {
        private const string KafkaTopicName = "foo";
        private readonly KafkaFixture fixture;

        public KafkaFixtureTests(KafkaFixture fixture, ITestOutputHelper output)
        {
            Console.SetOut(new ConsoleXunitAdapter(output));
            this.fixture = fixture;
            this.fixture.Start(new KafkaFixtureOptions
            {
                Topics = new List<KafkaTopic>
                {
                    new KafkaTopic
                    {
                        Name = KafkaTopicName
                    }
                }
            }, new ZooKeeperOptions()).Wait();
        }

        [Fact]
        public async Task KafkaIsReachable()
        {
            Assert.True(this.fixture.ContainerStarting);
            Assert.True(this.fixture.ContainerStarted);
            const int messagesToSend = 100;

            CancellationTokenSource cts = new CancellationTokenSource();
            var consume = Task.Run(() => this.consumeFromTopic(KafkaTopicName, cts.Token), cts.Token);
            await this.publishToTopic(KafkaTopicName, messagesToSend, this.fixture.Kafka.Endpoints.First());
            await Task.Delay(TimeSpan.FromSeconds(10));
            cts.Cancel();
            int messagesConsumed = await consume;

            Assert.Equal(messagesToSend, messagesConsumed);
        }

        [Fact]
        public async Task DisposeKillsKafka()
        {
            var kafkaFixture = new KafkaFixture();
            await kafkaFixture.Start(new KafkaFixtureOptions
            {
                Topics = new List<KafkaTopic>() { new KafkaTopic { Name = KafkaTopicName } }
            }, new ZooKeeperOptions());
            Assert.True(kafkaFixture.ContainerStarting);
            Assert.True(kafkaFixture.ContainerStarted);
            kafkaFixture.Dispose();

            var timeout = Task.Delay(TimeSpan.FromSeconds(10));
            await Task.WhenAny(new[]
            {
                timeout,
                this.publishToTopic(KafkaTopicName, 1, kafkaFixture.KafkaEndpoint)
            });

            Assert.True(timeout.IsCompletedSuccessfully, "Published to Kafka despite it being Disposed");
        }


        private int consumeFromTopic(string kafkaTopicName, CancellationToken cancellationToken)
        {
            var conf = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = this.fixture.KafkaEndpoint.ToString(),
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                c.Subscribe(kafkaTopicName);
                int consumedMessages = 0;
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        c.Consume(cancellationToken);
                        consumedMessages++;
                    }
                }
                catch (OperationCanceledException)
                {
                    c.Close();
                }

                return consumedMessages;
            }
        }

        private async Task publishToTopic(string kafkaTopicName, int messagesToSend, HostEndpoint endpoint)
        {
            var conf = new ProducerConfig { BootstrapServers = endpoint.ToString() };

            using (var p = new ProducerBuilder<Null, string>(conf).Build())
            {
                for (int i = 0; i < messagesToSend; ++i)
                {
                    await p.ProduceAsync(kafkaTopicName, new Message<Null, string> { Value = i.ToString() });
                }

                p.Flush(TimeSpan.FromSeconds(10));
            }
        }
    }
}
