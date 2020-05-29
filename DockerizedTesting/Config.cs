using DockerizedTesting.Containers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DockerizedTesting
{
    public class DockerTestConfig
    {
        public static IServiceProvider GetServiceProvider() //TODO: Sort this out
            => new ServiceCollection()
            .AddSingleton<IDockerClientProvider, DockerClientProvider>()
            .AddSingleton<IContainerActions, ContainerActions>()
            .BuildServiceProvider();

    }
}
