using Docker.DotNet;
using Docker.DotNet.Models;
using DockerizedTesting.ImageProviders;
using DockerizedTesting.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockerizedTesting.Containers
{
    public class LocalContainerActions : IContainerActions
    {
        protected readonly UniquePortProvider uniquePortProvider;
        protected readonly string hostname = "localhost"; // Poss override for vnetting
        private readonly IDockerClientProvider dockerClientProvider;

        public LocalContainerActions(IDockerClientProvider dockerClientProvider)
        {
            this.uniquePortProvider = new UniquePortProvider(this);
            this.dockerClientProvider = dockerClientProvider;
        }
        public virtual HostEndpoint[] ReservePorts(int count) =>
            Enumerable.Range(0, count)
            .Select(_ => this.uniquePortProvider.GetPort())
            .Select(p => new HostEndpoint(this.hostname, p))
            .ToArray();

        public virtual async Task KillZombieContainersBoundToPorts(int[] ports)
        {
            using var dockerClient = this.dockerClientProvider.GetDockerClient();
            var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });
            var toKill = containers.Where(c => c.Ports.Any(p => ports.Contains(p.PublicPort)));
            await Task.WhenAll(toKill.Select(c =>
                dockerClient.Containers.StopContainerAsync(c.ID, new ContainerStopParameters { WaitBeforeKillSeconds = 1 })));
        }

        public virtual async Task<string> StartContainer(CreateContainerParameters containerParameters, IDockerImageProvider imageProvider, string containerName, CancellationToken cancel = default)
        {
            using var dockerClient = this.dockerClientProvider.GetDockerClient();
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
            ContainerHost.Instance.ContainerIds.TryAdd(containerId, dockerClient.Configuration.EndpointBaseUri);

            ContainerInspectResponse containerInfo = null;
            do
            {
                try
                {
                    await Task.Delay(500, cancel);
                    containerInfo = await dockerClient.Containers.InspectContainerAsync(containerId, cancel);

                    if (!string.IsNullOrEmpty(containerInfo.State.Error) || containerInfo.State.Dead || containerInfo.State.OOMKilled)
                    {
                        throw new ContainerStartException(containerInfo.State);
                    }
                }
                catch (TaskCanceledException) { }
            } while (containerInfo != null && !containerInfo.State.Running && !cancel.IsCancellationRequested);
            return containerId;
        }

        public virtual void StopContainer(string containerId)
        {
            using var dockerClient = this.dockerClientProvider.GetDockerClient();
            var container = dockerClient.Containers.InspectContainerAsync(containerId).Result;
            if (container.State.Running)
            {
                dockerClient.Containers.KillContainerAsync(containerId, new ContainerKillParameters()).GetAwaiter().GetResult();
            }
        }

        private const string DefaultNetwork = "bridge";
        private string network = DefaultNetwork;
        public async Task DockerBridgeNetwork(bool enable, string preferredName)
        {
            using var dockerClient = this.dockerClientProvider.GetDockerClient();
            bool currentlyEnabled = this.network != DefaultNetwork;
            if (enable == currentlyEnabled)
            {
                return;
            }
            const string testClientPid = "testClientPid";
            string pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();

            if (enable && !currentlyEnabled)
            {
                var nets = await dockerClient.Networks.ListNetworksAsync();
                string suffix = string.Empty;
                int counter = 0;
                bool foundExistingNet;
                while (foundExistingNet = nets.Any(n => n.Name == preferredName + suffix
                && (!n.Labels.ContainsKey(testClientPid) || n.Labels[testClientPid] != pid)))
                {
                    suffix = $"_{counter++}";
                }

                if (!foundExistingNet)
                {
                    var net = await dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
                    {
                        Name = preferredName + suffix,
                        Attachable = true,
                        Labels = new Dictionary<string, string>
                        {
                            {testClientPid, pid },
                        }
                    });
                    //netId = net.ID;
                    this.network = preferredName + suffix;
                }
                else
                {
                    var net = nets.Single(n => n.Name == preferredName + suffix);
                    //netId = net.ID;
                    this.network = net.Name;
                }
            }
        }

        public string GetDockerNetwork() => network;

        //TODO: remove idisposable
        public void Dispose()
        {
        //    this.Dispose(true);
        //    GC.SuppressFinalize(this);
        //}
        //protected virtual void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        this.dockerClient.Dispose();
        //    }
        }

        public TokenWithStats GetTokenWithStats(TimeSpan timeout)
        => new LoadAdjustedCancellationTokenSource(this.dockerClientProvider.GetDockerClient(), timeout).Token;

    }
}
