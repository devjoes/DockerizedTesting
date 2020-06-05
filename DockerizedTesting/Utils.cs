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
            if (new[] { '/' }.Concat(Path.GetInvalidFileNameChars()).Any(command.Contains))
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

        public static byte[] ReadPemKey(string key)
            => Convert.FromBase64String(
            string.Join(string.Empty,
            key.Split('\n')
                .Select(l => l.Replace("\r", string.Empty).Replace("\t", string.Empty)
                .Replace(" ", string.Empty).Trim())
                .Where(l => l.Length > 0 
                    && l.ToCharArray().All(i =>
                        (i >= 65 && i <= 90) || (i >= 97 && i <= 122) || (i >= 48 && i <= 57)
                        || i == '+' || i == '/' || i == '='
                ))

               )
            );
    }
}
