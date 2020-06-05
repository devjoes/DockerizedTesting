using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Xunit;

namespace DockerizedTesting.RabbitMQ.Tests
{
    public class RabbitMqFixtureTests : IClassFixture<RabbitMqFixture>
    {
        private readonly RabbitMqFixture fixture;

        public RabbitMqFixtureTests(RabbitMqFixture fixture)
        {
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