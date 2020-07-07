using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;

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
        
        private int success = 0;
        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Loopback, ports.First());
                Socket sender = new Socket(IPAddress.Loopback.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                await sender.ConnectAsync(remoteEP);
                sender.Dispose();
                this.success++;
		await Task.Delay(1000);
                return await Task.FromResult(this.success >= 5);
            }
            catch
            {
                this.success = 0;
                return await Task.FromResult(false);
            }
        }

    }
}
