using Docker.DotNet;
using Docker.DotNet.Models;
using DockerizedTesting.ImageProviders;
using DockerizedTesting.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockerizedTesting.Containers
{
    public class LocalContainerActions : IContainerActions
    {
        protected readonly IDockerClientProvider dockerClientProvider;
        protected readonly UniquePortProvider uniquePortProvider;
        protected readonly string hostname = "localhost"; // Poss override for vnetting

        public LocalContainerActions(IDockerClientProvider dockerClientProvider)
        {
            this.dockerClientProvider = dockerClientProvider;
            this.uniquePortProvider = new UniquePortProvider(this);
        }
        public virtual HostEndpoint[] GetEndpoints(int count) =>
            Enumerable.Range(0, count)
            .Select(_ => this.uniquePortProvider.GetPort())
            .Select(p => new HostEndpoint(this.hostname, p))
            .ToArray();

        public virtual async Task KillZombieContainersBoundToPorts(int[] ports)
        {
            using (var dockerClient = this.dockerClientProvider.GetDockerClient())
            {
                var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true
                });
                var toKill = containers.Where(c => c.Ports.Any(p => ports.Contains(p.PublicPort)));
                await Task.WhenAll(toKill.Select(c =>
                    dockerClient.Containers.StopContainerAsync(c.ID, new ContainerStopParameters { WaitBeforeKillSeconds = 1 })));
            }
        }

        public virtual async Task<string> StartContainer(CreateContainerParameters containerParameters, IDockerImageProvider imageProvider, string containerName)
        {
            using (var dockerClient = this.dockerClientProvider.GetDockerClient())
            {
                containerParameters.Image = await imageProvider.GetImage(dockerClient);

                var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
                var existingContainer = containers.SingleOrDefault(c => c.Names.Contains("/" + containerName));

                string containerId = null;
                if (existingContainer != null)
                {
                    containerId = existingContainer.ID;
                    if (existingContainer.State == "running")
                    {
                        return containerId;
                    }
                }
                if (containerId == null)
                {
                    containerParameters.Name = containerName;
                    var container = await dockerClient.Containers.CreateContainerAsync(containerParameters);
                    containerId = container.ID;
                }

                await dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                ContainerHost.Instance.ContainerIds.TryAdd(containerId, dockerClientProvider.DockerUri);
                return containerId;
            }
        }

        public virtual void StopContainer(string containerId)
        {
            using (var dockerClient = this.dockerClientProvider.GetDockerClient())
            {
                var container = dockerClient.Containers.InspectContainerAsync(containerId).Result;
                if (container.State.Running)
                {
                    dockerClient.Containers.KillContainerAsync(containerId, new ContainerKillParameters()).Wait();
                }
            }
        }
    }
}
