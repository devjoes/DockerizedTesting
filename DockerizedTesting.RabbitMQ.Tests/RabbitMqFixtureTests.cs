using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DockerizedTesting.Tests.Containers;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace DockerizedTesting.RabbitMQ.Tests
{
    public class RabbitMqFixtureTests : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture fixture;

        public RabbitMqFixtureTests(RabbitMqFixture fixture, ITestOutputHelper output)
        {
            Console.SetOut(new ConsoleXunitAdapter(output));
            fixture.Start().GetAwaiter().GetResult();
            this.fixture = fixture;
        }

        [Fact]
        public void RabbitIsReachable()
        {
            var factory = this.fixture.SetupConnectionFactory(new ConnectionFactory());
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "hello",
                        durable: false,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    string message = "Hello World!";
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                        routingKey: "hello",
                        basicProperties: null,
                        body: body);
                }
            }
        }
    }

}