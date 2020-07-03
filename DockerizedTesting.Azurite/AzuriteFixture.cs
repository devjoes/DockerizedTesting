using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace DockerizedTesting.Azurite
{
    public class AzuriteFixture : BaseFixture<AzuriteFixtureOptions>
    {
        public AzuriteFixture() : base("azurite", 2)
        {
        }

        public string BlobConnectionString => "DefaultEndpointsProtocol=http;AccountName=" + this.Options.StorageAccountName + ";AccountKey=" + this.Options.StorageAccountKey + ";BlobEndpoint=http://localhost:" + this.Ports.First() + "/"+this.Options.StorageAccountName+";";

        public Task Start()
        {
            return this.Start(new AzuriteFixtureOptions());
        }

        public override Task Start(AzuriteFixtureOptions options)
        {
            return base.Start(options);
        }

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            var cmd = new List<string>();
            // Azurite requires that the ports match - not sure why
            cmd.AddRange(new[] { "azurite",
                "--blobHost", "0.0.0.0", "--blobPort", ports.First().ToString(),
                "--queueHost", "0.0.0.0", "--queuePort", ports.Last().ToString()
            });

            var vols = new Dictionary<string, EmptyStruct>();
            var mounts = new List<Mount>();
            if (!string.IsNullOrEmpty(this.Options.Workspace))
            {
                if (!Directory.Exists(this.Options.Workspace))
                {
                    throw new DirectoryNotFoundException(this.Options.Workspace);
                }

                const string workspace = "/workspace";
                cmd.Add("-l");
                cmd.Add("/" + workspace);
                mounts.Add(new Mount { Source = this.Options.Workspace, Target = workspace, Type = "bind" });
                vols.Add(workspace, default);
            }
            if (!string.IsNullOrEmpty(this.Options.DebugLog))
            {
                File.Create(this.Options.DebugLog).Dispose();
                const string debug = "/debug";
                cmd.Add("-d");
                cmd.Add("/" + debug);
                mounts.Add(new Mount { Source = this.Options.DebugLog, Target = debug, Type = "bind" });
                vols.Add(debug, default);
            }

            if (this.Options.Loose)
            {
                cmd.Add("--loose");
            }

            int[] containerPorts = ports;

            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = containerPorts.ToDictionary<int, String, EmptyStruct>(p => p.ToString(), _ => default),
                    Volumes = vols,
                    Cmd = cmd,
                    Env = new List<string>(new[]
                    {
                        "AZURITE_ACCOUNTS="+ this.Options.StorageAccountName+":"+this.Options.StorageAccountKey,
                    })
                })
            {
                HostConfig = Utils.HostWithBoundPorts(ports.ToArray(), h => h.Mounts = mounts.ToArray(), containerPorts)
            };
        }

        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var resp = await client.GetAsync(
                        $"http://localhost:{ports.First()}/{this.Options.StorageAccountName}/?comp=list");
                    return resp.StatusCode == HttpStatusCode.Forbidden;
                }
            }
            catch
            {
                return false;
            }
        }

    }
}
