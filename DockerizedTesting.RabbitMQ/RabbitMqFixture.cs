using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using RabbitMQ.Client;

namespace DockerizedTesting.RabbitMQ
{
    public class RabbitMqFixture : BaseFixture<RabbitMqFixtureOptions>
    {
        public RabbitMqFixture() : base("rabbitmq", 4)
        {
        }

        public Task Start()
        {
            return this.Start(new RabbitMqFixtureOptions());
        }

        public override Task Start(RabbitMqFixtureOptions options)
        {
            return base.Start(options);
        }

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            int[] containerPorts = new []{ 5672, 4369, 25672 ,15672};
            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = containerPorts.ToDictionary<int,String,EmptyStruct>(p => p.ToString(), _ => default),
                    Env = new List<string>(new[]
                    {
                        "RABBITMQ_USERNAME="+ this.Options.UserName,
                        "RABBITMQ_PASSWORD="+ this.Options.Password,
                    })
                })
            {
                HostConfig = Utils.HostWithBoundPorts(ports, containerPorts)
            };
        }

        public ConnectionFactory SetupConnectionFactory(ConnectionFactory connectionFactory)
        {
            connectionFactory.HostName = "localhost";
            connectionFactory.Port = Ports.First();
            connectionFactory.UserName = this.Options.UserName;
            connectionFactory.Password = this.Options.Password;
            return connectionFactory;
        }

        private int success = 0;
        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            try
            {
                var factory = this.SetupConnectionFactory(new ConnectionFactory());
                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        if (channel.IsClosed)
                        {
                            throw new InvalidOperationException();
                        }

                    }
                }
                this.success++;
                return await Task.FromResult(this.success >= 5); // Rabbit seems to work then stop?
            }
            catch
            {
                this.success = 0;
                return await Task.FromResult(false);
            }
        }

    }
}
