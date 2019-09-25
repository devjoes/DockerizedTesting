using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DockerizedTesting
{
    public abstract class BaseFixture<T> : IBaseFixture where T : FixtureOptions
    {
        protected BaseFixture(string containerName, int exposedPorts)
        {
            this.ContainerName = containerName;
            this.DockerClient = this.DockerClientProvider.GetDockerClient();
            this.Ports = Enumerable.Range(0, exposedPorts)
                .Select(_ => this.GetPort()).ToArray();
        }

        protected readonly string ContainerName;
        protected readonly DockerClient DockerClient;

        public IDockerClientProvider DockerClientProvider { get; set; } = new DockerClientProvider();
        public string ContainerId { get; protected set; }
        public int[] Ports { get; protected set; }
        public bool ContainerStarting { get; protected set; }
        public bool ContainerStarted { get; protected set; }

        private string uniqueContainerName;

        public string UniqueContainerName =>
            this.uniqueContainerName ?? (this.uniqueContainerName =
                $"{this.ContainerName}_{string.Join("_", this.Ports)}_{this.GetContainerParameters(this.Ports).GetHashCode()}"
            );

        protected int GetPort()
        {
            bool gotPort;
            int port;
            do
            {
                port = Interlocked.Increment(ref Utils.MinPort) - 1;
                try
                {
                    var listener = new TcpListener(IPAddress.Any, port);
                    listener.Start();
                    listener.Stop();
                    gotPort = true;
                }
                catch (SocketException)
                {
                    gotPort = false;
                }
            } while (!gotPort);

            return port;
        }
        
        protected async Task WaitForContainer(int[] ports)
        {
            this.ContainerStarted = false;
            int attempts = 0;
            do
            {
                this.ContainerStarted = await this.IsContainerRunning(ports);
                await Task.Delay(this.Options.DelayMs);
            } while (!this.ContainerStarted && attempts++ <= this.Options.MaxRetries);
        }

        protected async Task StartContainer(int[] ports)
        {
            var containerParameters = this.GetContainerParameters(ports);
            await this.PullImage(containerParameters.Image);
            
            var containers = await this.DockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            var existingContainer = containers.SingleOrDefault(c => c.Names.Contains("/" + this.UniqueContainerName));

            if (existingContainer != null)
            {
                this.ContainerId = existingContainer.ID;
                if (existingContainer.State == "running")
                {
                    return;
                }
            }
            if (this.ContainerId == null)
            {
                containerParameters.Name = this.UniqueContainerName;
                var container = await this.DockerClient.Containers.CreateContainerAsync(containerParameters);
                this.ContainerId = container.ID;
            }

            await this.DockerClient.Containers.StartContainerAsync(this.ContainerId, new ContainerStartParameters());
            this.ContainerStarting = true;
            this.Options.ContainerHost.ContainerIds.TryAdd(this.ContainerId, this.DockerClientProvider.DockerUri);
        }

        protected async Task PullImage(string image)
        {
            var splitImage = image.Split(':');
            if (!(await this.DockerClient.Images.ListImagesAsync(new ImagesListParameters{MatchName = image})).Any())
            {
                await this.DockerClient.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = splitImage.First(),
                    Tag = splitImage.Length == 1 ? "latest" : splitImage.Last()
                }, new AuthConfig(), new Progress<JSONMessage>());
            }
        }


        protected abstract CreateContainerParameters GetContainerParameters(int[] ports);
        protected abstract Task<bool> IsContainerRunning(int[] ports);

        public virtual async Task Start(T opts)
        {
            this.Options = opts;
            await this.StartContainer(this.Ports);
            await this.WaitForContainer(this.Ports);
        }

        public bool IsDisposed { get; protected set; }
        public T Options;

        public virtual void Dispose()
        {
            //TODO: When .net 3 is out create a watchdog process using https://laurentkempe.com/2019/02/18/dynamically-compile-and-run-code-using-dotNET-Core-3.0/
            if (this.IsDisposed || this.DockerClient == null || this.ContainerId == null)
            {
                return;
            }

            var container = this.DockerClient.Containers.InspectContainerAsync(this.ContainerId).Result;
            if (container.State.Running)
            {
                this.DockerClient.Containers.KillContainerAsync(this.ContainerId, new ContainerKillParameters()).Wait();
            }
            Task.Delay(this.Options.DelayMs).Wait();
            this.DockerClient.Dispose();
            this.IsDisposed = true;
        }

    }
}
