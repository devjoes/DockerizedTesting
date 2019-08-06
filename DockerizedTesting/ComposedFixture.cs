using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerizedTesting
{
    public abstract class ComposedFixture: IDisposable
    {
        protected string networkName;
        protected readonly DockerClient DockerClient;
        public BlockingCollection<IBaseFixture> Fixtures { get; }

        protected ComposedFixture(string networkName)
        {
            this.networkName = networkName;
            this.Fixtures = new BlockingCollection<IBaseFixture>();
            this.DockerClient = new DockerClientConfiguration(this.DockerUri).CreateClient();
            this.createNetworkIfNotExists(this.networkName);
        }

        protected async Task<IBaseFixture> Instantiate<T>(Func<T, ComposedFixture, Task> start) where T : IBaseFixture, new()
        {
            var fixture = new T();
            if (fixture is IFixtureWithCustomNetwork networkedFixture)
            {
                networkedFixture.NetworkName = this.networkName;
            }

            this.Fixtures.Add(fixture);
            await start(fixture, this);
            
            return fixture;
        }

        private void createNetworkIfNotExists(string name)
        {
            if (this.DockerClient.Networks.ListNetworksAsync(new NetworksListParameters()).Result.All(n => n.Name != name))
            {
                this.DockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters { Name = name }).Wait();
            }
        }

        protected Uri DockerUri =>
            new Uri(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "npipe://./pipe/docker_engine"
                    : "unix:///var/run/docker.sock"
            );

        public void Dispose()
        {
            Task.WaitAll(this.Fixtures.Select(f => Task.Run(f.Dispose)).ToArray());
            this.DockerClient?.Dispose();
        }
    }


    public class ComposedFixture<T1, T2, T3, T4,T5> : ComposedFixture
        where T1 : IBaseFixture, new()
        where T2 : IBaseFixture, new()
        where T3 : IBaseFixture, new()
        where T4 : IBaseFixture, new()
        where T5 : IBaseFixture, new()
    {
        public ComposedFixture(string networkName) : base(networkName)
        {
        }

        public virtual async Task Start(Func<T1,ComposedFixture,Task> start1, Func<T2,ComposedFixture,Task> start2, Func<T3,ComposedFixture,Task> start3, Func<T4,ComposedFixture,Task> start4, Func<T5,ComposedFixture,Task> start5)
        {
            await Task.WhenAll(new[]
            {
                this.Instantiate(start1),
                this.Instantiate(start2),
                this.Instantiate(start3),
                this.Instantiate(start4),
                this.Instantiate(start5)
            });
        }
    }

    public class ComposedFixture<T1, T2, T3, T4> : ComposedFixture
        where T1 : IBaseFixture, new()
        where T2 : IBaseFixture, new()
        where T3 : IBaseFixture, new()
        where T4 : IBaseFixture, new()
    {
        public virtual async Task Start( Func<T1,ComposedFixture,Task> start1, Func<T2,ComposedFixture,Task> start2, Func<T3,ComposedFixture,Task> start3, Func<T4,ComposedFixture,Task> start4)
        {
            await Task.WhenAll(new[]
            {
                this.Instantiate(start1),
                this.Instantiate(start2),
                this.Instantiate(start3),
                this.Instantiate(start4)
            });
        }

        public ComposedFixture(string networkName) : base(networkName)
        {
        }
    }

    public class ComposedFixture<T1, T2, T3> : ComposedFixture
        where T1 : IBaseFixture, new()
        where T2 : IBaseFixture, new()
        where T3 : IBaseFixture, new()
    {
        public virtual async Task Start( Func<T1,ComposedFixture,Task> start1, Func<T2,ComposedFixture,Task> start2, Func<T3,ComposedFixture,Task> start3)
        {
            await Task.WhenAll(new[]
            {
                this.Instantiate(start1),
                this.Instantiate(start2),
                this.Instantiate(start3)
            });
        }

        public ComposedFixture(string networkName) : base(networkName)
        {
        }
    }

    public class ComposedFixture<T1, T2> : ComposedFixture
        where T1 : IBaseFixture, new()
        where T2 : IBaseFixture, new()
    {
        public virtual async Task Start(Func<T1,ComposedFixture,Task> start1, Func<T2,ComposedFixture,Task> start2)
        {
            await Task.WhenAll(new[]
            {
                this.Instantiate(start1),
                this.Instantiate(start2)
            });
        }

        public ComposedFixture(string networkName) : base(networkName)
        {
        }
    }
    
}
