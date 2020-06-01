using DockerizedTesting.Models;
using System;
using System.Net;

namespace DockerizedTesting
{
    public interface IBaseFixture : IDisposable
    {
        bool IsDisposed { get; }
        string ContainerId { get; }
        HostEndpoint[] Endpoints { get; }
        bool ContainerStarting { get; }
        bool ContainerStarted { get; }
        string UniqueContainerName { get; }
    }
}