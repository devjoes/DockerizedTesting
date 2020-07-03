using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docker.DotNet.Models;

namespace DockerizedTesting
{
    public static class Utils
    {
        public const int MinPort = 12000;
        
        public static HostConfig HostWithBoundPorts(int[] hostPorts, params int[] containerPorts)
            => new HostConfig
            {
                PortBindings = containerPorts.ToDictionary(
                    k => k.ToString(),
                    v => (IList<PortBinding>)new List<PortBinding>
                    {
                        new PortBinding
                        {
                            HostPort = hostPorts[Array.IndexOf(containerPorts, v) % hostPorts.Length].ToString()
                        }
                    })
            };

        public static HostConfig HostWithBoundPorts(int[] hostPorts, Action<HostConfig> extraConfig, params int[] containerPorts)
        {
            var config = HostWithBoundPorts(hostPorts, containerPorts);
            extraConfig(config);
            return config;
        }
    }
}
