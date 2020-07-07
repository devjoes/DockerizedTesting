using System;
using System.Linq;
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
            fixture.Start().Wait();
            this.fixture = fixture;
        }

        [Fact]
        public void RabbitIsReachable()
        {
            var factory = new ConnectionFactory();
            factory.HostName = "localhost";
            factory.Port = this.fixture.Ports.First();
            factory.UserName = this.fixture.Options.UserName;
            factory.Password = this.fixture.Options.Password;
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