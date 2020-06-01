using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        public static bool CommandExists(string command)
        {
            return CommandExists(command, Environment.GetEnvironmentVariable("PATH"));
        }
        public static bool CommandExists(string command, string path)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }
            if(new[] { '/' }.Concat(Path.GetInvalidFileNameChars()).Any(command.Contains))
            {
                throw new ArgumentException("Invalid filename", nameof(command));
            }
            try
            {
                var search = new[] { command };
                var isWin = Environment.OSVersion.Platform.HasFlag(PlatformID.Win32NT);
                if (isWin)
                {
                    search = new[] { command, $"{command}.???" };
                }

                return path
                    .Split(new[] { isWin ? ';' : ':' })
                    .Concat(new[] { Path.GetFullPath(".") })
                    .Select(p => p.Trim())
                    .Where(Directory.Exists)
                    .Select(p => new DirectoryInfo(p))
                    .Any(d => search.Any(s => d.GetFiles(s).Any()));
            }
            catch
            {
                return true;
            }
        }
    }
}
