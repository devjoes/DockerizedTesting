using DockerizedTesting.Containers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DockerizedTesting.Models
{
    public class GlobalConfig
    {
        public virtual IServiceProvider GetServiceProvider() => serviceProvider.Value;
        private static Lazy<IServiceProvider> serviceProvider = new Lazy<IServiceProvider>(
            () => new ServiceCollection()
            .AddSingleton<IDockerClientProvider, DockerClientProvider>()
            .AddSingleton<IContainerActions, LocalContainerActions>()
            .BuildServiceProvider());

    }
}
