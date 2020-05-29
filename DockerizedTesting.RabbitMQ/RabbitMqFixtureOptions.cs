using System;
using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.RabbitMQ
{
    public class RabbitMqFixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerHubImageProvider("bitnami/rabbitmq:latest");
        public override int DelayMs => 5000;
        public string UserName { get; set; } = "user";
        public string Password { get; set; } = "D0cK3rIz3d_T3sting!!";

    }
}
