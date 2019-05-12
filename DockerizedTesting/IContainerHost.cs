using System;
using System.Collections.Concurrent;

namespace DockerizedTesting
{
    /// <summary>
    /// This class takes care of removing containers, volumes and generally cleaning up. It should be a singleton.
    /// </summary>
    public interface IContainerHost : IDisposable
    {
        ConcurrentDictionary<string, Uri> ContainerIds { get; }
        bool RemoveContainersOnExit { get; set; }
    }
}