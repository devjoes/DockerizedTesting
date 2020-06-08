using Docker.DotNet;
using Docker.DotNet.Models;
using DockerizedTesting.Containers;
using DockerizedTesting.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DockerizedTesting
{
    public abstract class BaseFixture<T> : IBaseFixture where T : FixtureOptions
    {
        protected BaseFixture(string containerName, int exposedPorts) :
            this(containerName, exposedPorts, new GlobalConfig())
        { }

        protected BaseFixture(string containerName, int exposedPorts, GlobalConfig config)
        {
            var serviceProvider = config.GetServiceProvider();
            this.globalConfig = config;
            this.actions = serviceProvider.GetService<IContainerActions>();
            this.ContainerName = containerName;
            this.Endpoints = this.actions.ReservePorts(exposedPorts);
        }

        private readonly GlobalConfig globalConfig;
        private readonly IContainerActions actions;
        protected readonly string ContainerName;

        public string ContainerId { get; protected set; }
        public HostEndpoint[] Endpoints { get; protected set; }
        public bool ContainerStarting { get; protected set; }
        public bool ContainerStarted { get; protected set; }

        private string uniqueContainerName;

        public string UniqueContainerName =>
            this.uniqueContainerName ?? (this.uniqueContainerName =
                $"{this.ContainerName}_{string.Join("_", this.Endpoints.Select(e => e.Port))}_{this.GetContainerParameters(this.Endpoints.Select(e => e.Port).ToArray()).GetHashCode()}"
            );

        protected async Task WaitForContainer(HostEndpoint[] endpoints)
        {
            this.ContainerStarted = false;
            int attempts = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            do
            {
                this.ContainerStarted = await this.IsContainerRunning(endpoints);
                await Task.Delay(this.Options.DelayMs);
            } while (!this.ContainerStarted && attempts++ <= this.Options.MaxRetries);
            sw.Stop();
            if (!this.ContainerStarted)
            {
                throw new TimeoutException($"Container failed to start after {sw.Elapsed} ({this.Options.MaxRetries} attempts)");
            }
        }

        protected async Task<string> StartContainer(IEnumerable<int> ports)
        {
            var paramaters = GetContainerParameters(ports.ToArray());
            var cancel = new CancellationTokenSource(this.Options.CreationTimeoutMs != 0
                ? this.Options.CreationTimeoutMs
                : this.globalConfig.DefaultCreationTimeoutMs);
            return await this.actions.StartContainer(paramaters, Options.DelayedScheduling, Options.ImageProvider, this.UniqueContainerName, cancel.Token);
        }

        protected abstract CreateContainerParameters GetContainerParameters(int[] ports);
        protected abstract Task<bool> IsContainerRunning(HostEndpoint[] endpoints);

        public virtual async Task Start(T opts)
        {
            if (this.ContainerStarted)
            {
                //TODO: Currently this is shared between all tests in a fixture. Add option to restart on each run
                return;
            }
            this.Options = opts;
            this.ContainerStarted = false;
            this.ContainerStarting = true;
            this.ContainerId = await this.StartContainer(this.Endpoints.Select(e => e.Port));
            await this.WaitForContainer(this.Endpoints);
        }

        public bool IsDisposed { get; protected set; }
        public T Options;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || this.IsDisposed || this.ContainerId == null)
            {
                return;
            }
            this.actions.StopContainer(this.ContainerId);

            Task.Delay(this.Options.DelayMs).Wait();
            this.IsDisposed = true;
        }

    }
}
