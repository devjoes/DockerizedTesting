using System;
using System.Runtime.InteropServices;
using Docker.DotNet;

namespace DockerizedTesting.Containers
{
    public interface IDockerClientProvider
    {
        DockerClient GetDockerClient();
        Uri DockerUri { get; }
    }

    public class DockerClientProvider : IDockerClientProvider
    {
        public virtual DockerClient GetDockerClient() =>
            new DockerClientConfiguration(this.DockerUri).CreateClient();

        public virtual Uri DockerUri =>
            new Uri(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "npipe://./pipe/docker_engine"
                    : "unix:///var/run/docker.sock"
            );
    }
}