using Docker.DotNet.Models;
using DockerizedTesting.ImageProviders;
using DockerizedTesting.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DockerizedTesting.Containers
{
    public interface IContainerActions
    {
        Task KillZombieContainersBoundToPorts(int[] ports);
        Task<string> StartContainer(CreateContainerParameters containerParams, IDockerImageProvider imageProvider, string containerName);
        HostEndpoint[] GetEndpoints(int count);
        void StopContainer(string containerId);
    }
}
