using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerizedTesting
{
    public abstract class BaseFixture<T> : IDisposable where T : FixtureOptions
    {
        protected BaseFixture(string containerName, int exposedPorts)
        {
            this.ContainerName = containerName;
            this.DockerClient = new DockerClientConfiguration(this.DockerUri).CreateClient();
            this.Ports = Enumerable.Range(0, exposedPorts)
                .Select(_ => this.GetPort()).ToArray();
        }

        protected readonly string ContainerName;
        protected readonly DockerClient DockerClient;
        
        public string ContainerId { get; protected set; }
        public int[] Ports { get; protected set; }
        public bool ContainerStarting { get; protected set; }
        public bool ContainerStarted { get; protected set; }
        
        protected Uri DockerUri =>
            new Uri(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "npipe://./pipe/docker_engine"
                    : "unix:///var/run/docker.sock"
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
                await Task.Delay(this.options.DelayMs);
            } while (this.ContainerStarted && attempts++ <= this.options.MaxRetries);
        }

        protected async Task StartContainer(int[] ports)
        {
            var containerParameters = this.GetContainerParameters(ports);
            await this.PullImage(containerParameters.Image);
            
            string name = $"{this.ContainerName}_{string.Join("_", ports)}_{containerParameters.GetHashCode()}";
            var containers = await this.DockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            var existingContainer = containers.SingleOrDefault(c => c.Names.Contains("/" + name));

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
                containerParameters.Name = name;
                var container = await this.DockerClient.Containers.CreateContainerAsync(containerParameters);
                this.ContainerId = container.ID;
            }

            this.ContainerStarting = await this.DockerClient.Containers.StartContainerAsync(this.ContainerId, new ContainerStartParameters());
            this.options.ContainerHost.ContainerIds.TryAdd(this.ContainerId, this.DockerUri);
        }

        protected async Task PullImage(string image)
        {
            var splitImage = image.Split(':');
            //todo: thread safety
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
            this.options = opts;
            await this.StartContainer(this.Ports);
            await this.WaitForContainer(this.Ports);
        }

        private bool isDisposed;
        private T options;

        public virtual void Dispose()
        {
            if (this.isDisposed || this.DockerClient == null)
            {
                return;
            }

            var container = this.DockerClient.Containers.InspectContainerAsync(this.ContainerId).Result;
            if (container.State.Running)
            {
                this.DockerClient.Containers.KillContainerAsync(this.ContainerId, new ContainerKillParameters()).Wait();
            }
            Task.Delay(this.options.DelayMs).Wait();
            this.DockerClient.Dispose();
            this.isDisposed = true;
        }

    }
}
