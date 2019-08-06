using System;

namespace DockerizedTesting
{
    public interface IBaseFixture : IDisposable
    {
        bool IsDisposed { get; }
        string ContainerId { get; }
        int[] Ports { get; }
        bool ContainerStarting { get; }
        bool ContainerStarted { get; }
        string UniqueContainerName { get; }
    }
}