using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using DockerizedTesting.Containers;
using DockerizedTesting.Models;

namespace DockerizedTesting
{
    public abstract class ComposedFixture : IDisposable
    {
        protected string preferredNetworkName;
        public CompositeDisposable DisposeFixtures { get; }
        public BlockingCollection<IBaseFixture> Fixtures { get; }

        private IContainerActions actions;

        protected ComposedFixture(string preferredNetworkName) : this(preferredNetworkName, new GlobalConfig())
        {
        }

        protected ComposedFixture(string preferredNetworkName, GlobalConfig config)
        {
            var serviceProvider = config.GetServiceProvider();
            this.preferredNetworkName = preferredNetworkName;
            this.DisposeFixtures = new CompositeDisposable();
            this.Fixtures = new BlockingCollection<IBaseFixture>();
            this.actions = serviceProvider.GetService<IContainerActions>();
        }

        protected async Task<IBaseFixture> Instantiate<T>(Func<T, ComposedFixture, Task> start) where T : IBaseFixture, new()
        {
            var fixture = new T();
            if (fixture is IFixtureWithCustomNetwork networkedFixture)
            {
                networkedFixture.NetworkName = this.preferredNetworkName;
            }

            this.DisposeFixtures.Add(fixture);
            this.Fixtures.Add(fixture);
            await start(fixture, this);

            return fixture;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.actions.Dispose();
                this.DisposeFixtures.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }


    public class ComposedFixture<T1, T2, T3, T4, T5> : ComposedFixture
        where T1 : IBaseFixture, new()
        where T2 : IBaseFixture, new()
        where T3 : IBaseFixture, new()
        where T4 : IBaseFixture, new()
        where T5 : IBaseFixture, new()
    {
        public ComposedFixture(string preferredNetworkName, GlobalConfig config) : base(preferredNetworkName, config)
        { }
        public ComposedFixture(string preferredNetworkName) : base(preferredNetworkName)
        {
        }

        public virtual async Task Start(Func<T1, ComposedFixture, Task> start1, Func<T2, ComposedFixture, Task> start2, Func<T3, ComposedFixture, Task> start3, Func<T4, ComposedFixture, Task> start4, Func<T5, ComposedFixture, Task> start5)
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
        public virtual async Task Start(Func<T1, ComposedFixture, Task> start1, Func<T2, ComposedFixture, Task> start2, Func<T3, ComposedFixture, Task> start3, Func<T4, ComposedFixture, Task> start4)
        {
            await Task.WhenAll(new[]
            {
                this.Instantiate(start1),
                this.Instantiate(start2),
                this.Instantiate(start3),
                this.Instantiate(start4)
            });
        }

        public ComposedFixture(string preferredNetworkName, GlobalConfig config) : base(preferredNetworkName, config)
        { }
        public ComposedFixture(string preferredNetworkName) : base(preferredNetworkName)
        {
        }
    }

    public class ComposedFixture<T1, T2, T3> : ComposedFixture
        where T1 : IBaseFixture, new()
        where T2 : IBaseFixture, new()
        where T3 : IBaseFixture, new()
    {
        public virtual async Task Start(Func<T1, ComposedFixture, Task> start1, Func<T2, ComposedFixture, Task> start2, Func<T3, ComposedFixture, Task> start3)
        {
            await Task.WhenAll(new[]
            {
                this.Instantiate(start1),
                this.Instantiate(start2),
                this.Instantiate(start3)
            });
        }

        public ComposedFixture(string preferredNetworkName, GlobalConfig config) : base(preferredNetworkName, config)
        { }
        public ComposedFixture(string preferredNetworkName) : base(preferredNetworkName)
        {
        }
    }

    public class ComposedFixture<T1, T2> : ComposedFixture
        where T1 : IBaseFixture, new()
        where T2 : IBaseFixture, new()
    {
        public virtual async Task Start(Func<T1, ComposedFixture, Task> start1, Func<T2, ComposedFixture, Task> start2)
        {
            await Task.WhenAll(new[]
            {
                this.Instantiate(start1),
                this.Instantiate(start2)
            });
        }

        public ComposedFixture(string preferredNetworkName, GlobalConfig config) : base(preferredNetworkName, config)
        { }
        public ComposedFixture(string preferredNetworkName) : base(preferredNetworkName)
        {
        }
    }

}
