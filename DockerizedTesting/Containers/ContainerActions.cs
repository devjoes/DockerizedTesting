using Docker.DotNet.Models;
using DockerizedTesting.ImageProviders;
using DockerizedTesting.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockerizedTesting.Containers
{
    public interface IContainerActions :IDisposable
    {
        Task KillZombieContainersBoundToPorts(int[] ports);
        Task<string> StartContainer(CreateContainerParameters containerParams, FixtureOptions.DelayedSchedulingOptions delayedSchedulingOptions, IDockerImageProvider imageProvider, string containerName, CancellationToken cancel = default);
        HostEndpoint[] ReservePorts(int count);
        void StopContainer(string containerId);
        Task DockerBridgeNetwork(bool enable, string preferredName = "dockerized_testing");
        string GetDockerNetwork();
    }
}
