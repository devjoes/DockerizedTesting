using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerizedTesting.Containers
{

    /// <summary>
    /// This class takes care of removing containers, volumes and generally cleaning up. It should be a singleton.
    /// </summary>
    public interface IContainerHost : IDisposable
    {
        ConcurrentDictionary<string, Uri> ContainerIds { get; }
    }
    public class ContainerHost : IContainerHost
    {
        private ContainerHost()
        {
            AppDomain.CurrentDomain.ProcessExit += this.Exiting;
            AppDomain.CurrentDomain.DomainUnload += this.Exiting;
            this.ContainerIds = new ConcurrentDictionary<string, Uri>();
        }

        private void Exiting(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public static ContainerHost Instance { get; set; } = new ContainerHost();

        public ConcurrentDictionary<string, Uri> ContainerIds { get; }

        private void removeContainers()
        {
            Console.WriteLine("Cleaning up: " + string.Join(", ", this.ContainerIds.Keys));
            var clients = this.ContainerIds.Values.Distinct().ToDictionary(
                k => k,
                v => new DockerClientConfiguration(v).CreateClient());
            try
            {
                Task.WaitAll(
                    this.ContainerIds
                        .Where(kvp =>
                            !clients[kvp.Value].Containers.InspectContainerAsync(kvp.Key).Result.State.Running)
                        .Select(kvp =>
                            clients[kvp.Value].Containers.RemoveContainerAsync(kvp.Key, new ContainerRemoveParameters
                            {
                                Force = true,
                                RemoveVolumes = true
                            })).ToArray());
            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.Message);
            }

            foreach (var client in clients.Values)
            {
                client.Dispose();
            }
        }

        private bool disposed = false;
        public void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            this.removeContainers();
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        ~ContainerHost()
        {
            this.Dispose(false);
        }
    }
}
