using Docker.DotNet;
using Docker.DotNet.Models;
using DockerizedTesting.ImageProviders;
using DockerizedTesting.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DockerizedTesting.Containers
{
    public class LocalContainerActions : IContainerActions
    {
        protected readonly UniquePortProvider uniquePortProvider;
        private readonly DockerClient dockerClient;
        protected readonly string hostname = "localhost"; // Poss override for vnetting

        public LocalContainerActions(IDockerClientProvider dockerClientProvider)
        {
            this.uniquePortProvider = new UniquePortProvider(this);
            this.dockerClient = dockerClientProvider.GetDockerClient();
        }
        public virtual HostEndpoint[] ReservePorts(int count) =>
            Enumerable.Range(0, count)
            .Select(_ => this.uniquePortProvider.GetPort())
            .Select(p => new HostEndpoint(this.hostname, p))
            .ToArray();

        public virtual async Task KillZombieContainersBoundToPorts(int[] ports)
        {
            var containers = await this.dockerClient.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });
            var toKill = containers.Where(c => c.Ports.Any(p => ports.Contains(p.PublicPort)));
            await Task.WhenAll(toKill.Select(c =>
                this.dockerClient.Containers.StopContainerAsync(c.ID, new ContainerStopParameters { WaitBeforeKillSeconds = 1 })
                    .ContinueWith(_ => this.dockerClient.Containers.RemoveContainerAsync(c.ID,
                        new ContainerRemoveParameters { Force = true, RemoveVolumes = true },
                        CancellationToken.None))));
        }

        public const string labelDockerizedTesting = "dockerizedTesting";
        public virtual async Task<string> StartContainer(CreateContainerParameters containerParameters, FixtureOptions.DelayedSchedulingOptions delayedSchedulingOptions, IDockerImageProvider imageProvider, string containerName, CancellationToken cancel = default)
        {
            //todo: this needs splitting up and tidying up
            containerParameters.Image = await imageProvider.GetImage(this.dockerClient);

            var containers = (await this.dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true })).ToArray();

            containerParameters.Name = containerName;
            if (containerParameters.Labels == null)
            {
                containerParameters.Labels = new Dictionary<string, string>();
            }

            containers = (await this.dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true })).ToArray();
            var dupeContainers = containers.Where(c => c.Names.Select(n => n.Trim('/')).Contains(containerName)).ToArray();
            if (dupeContainers.Any())
            {
                //todo: fix this properly
                var dead = dupeContainers.Where(c => c.State != "running" && c.State != "created").ToArray();
                foreach (var c in dead)
                {
                    await this.dockerClient.Containers.RemoveContainerAsync(c.ID,
                        new ContainerRemoveParameters { RemoveVolumes = true, Force = true });
                }

                if (dead.Length < dupeContainers.Length)
                {
                    containerName += Guid.NewGuid().ToString().Remove(4);
                }
            }

            containerParameters.Labels.Add(labelDockerizedTesting, delayedSchedulingOptions.GetLabel(containerName + "-" + Guid.NewGuid().ToString()));
            var container = await this.dockerClient.Containers.CreateContainerAsync(containerParameters);
            var containerId = container.ID;

            var delay = await WaitTillContainerCanStart(
                async () =>
                    (await this.dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true }))
                    .Where(c => c.State == "created" || c.State == "running").ToArray()
                , containerId, delayedSchedulingOptions, cancel);
            if (delay.TotalSeconds > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Delayed running {containerName} by {delay}");
            }

            var success = await this.dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
            ContainerHost.Instance.ContainerIds.TryAdd(containerId, dockerClient.Configuration.EndpointBaseUri);

            ContainerInspectResponse containerInfo = null;
            do
            {
                try
                {
                    await Task.Delay(500, cancel);
                    containerInfo = await this.dockerClient.Containers.InspectContainerAsync(containerId, cancel);

                    if (!string.IsNullOrEmpty(containerInfo.State.Error) || containerInfo.State.Dead || containerInfo.State.OOMKilled)
                    {
                        throw new ContainerStartException(containerInfo.State);
                    }
                }
                catch (TaskCanceledException) { }
            } while (containerInfo != null && !containerInfo.State.Running && !cancel.IsCancellationRequested);
            return containerId;
        }

        public async static Task<TimeSpan> WaitTillContainerCanStart(Func<Task<IList<ContainerListResponse>>> getContainers, string containerId, FixtureOptions.DelayedSchedulingOptions delayedSchedulingOptions, CancellationToken cancellationToken)
        {
            return await WaitTillContainerCanStart(getContainers, containerId, delayedSchedulingOptions, cancellationToken, () => DateTime.UtcNow, true);
        }

        public async static Task<TimeSpan> WaitTillContainerCanStart(Func<Task<IList<ContainerListResponse>>> getContainers, string containerId,
            FixtureOptions.DelayedSchedulingOptions delayedSchedulingOptions, CancellationToken cancellationToken,
            Func<DateTime> getNow, bool waitForDelay)
        {
            // There is an edge case where two conflicting containers attempt to start at the same time.
            // Because neither has been created then the labels wont conflict and there will be a race condition.
            if (waitForDelay)
            {
                await Task.Delay(TimeSpan.FromSeconds(new Random().Next(1, 5)));
            }

            int deadlockCount = 0;

            var startTime = getNow();
            startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second); //remove ms

            (int max, DateTime from, DateTime to, string id, bool running)[] conflictingContainers = null;
            var delayTill = startTime;
            while (conflictingContainers == null ||
                    (conflictingContainers.Any()
//                   && (conflictingContainers.Length  > conflictingContainers.Min(c => c.max) ||
//                        conflictingContainers.Length  > delayedSchedulingOptions.MaxContainers)
))
            {
                var schedule = (await getContainers()).Where(c => c.Labels.ContainsKey(labelDockerizedTesting))
                    .Select<ContainerListResponse, (int max, DateTime from, DateTime to, string id, bool running)>(
                        c =>
                        {
                            var split = c.Labels[labelDockerizedTesting].Split('_');
                            return (int.Parse(split[0]), DateTime.Parse(split[1]), DateTime.Parse(split[2]), c.ID, c.State.Equals("running"));
                        })
                    .ToArray();
                if (conflictingContainers != null)
                {
                    var pause = 250;
                    if (conflictingContainers.Length > 1)
                    {
                        pause += new Random().Next(0, 500); // randomly back off until someone wins
                    }
                    await Task.Delay(pause);

                    if (delayedSchedulingOptions.MaxContainers == 0 && 
                        conflictingContainers.All(c => c.id != containerId && !c.running))
                    {
                        // This container doesn't conflict - others just conflict with it. They won't start but we can
                        return default;
                    }

                    //if (conflictingContainers.Length > 1 && conflictingContainers.All(c => !c.running))
                    //{
                    //    // We can get deadlocked on start because all containers are trying to start but none can.
                    //    // In that scenario the first by id starts. This will ensure that at least one starts then the rest will unlock.
                    //    if (conflictingContainers.Min(c => c.id) == containerId && deadlockCount++ > 5)
                    //    {
                    //        return TimeSpan.Zero;
                    //    }
                    //}
                    //else
                    //{
                    //    deadlockCount = 0;
                    //}
                    delayTill = delayTill.AddSeconds(1);
                }

                var toProcess = schedule.ToList().Where(i =>
                        i.id != containerId)
                    .ToList();

                toProcess.Add((
                    delayedSchedulingOptions.MaxContainers,
                    delayTill.AddSeconds(0 - delayedSchedulingOptions.SchedulingWindowBefore.TotalSeconds),
                    delayTill.AddSeconds(delayedSchedulingOptions.SchedulingWindowAfter.TotalSeconds), containerId, false));
                var containerCollisions = toProcess
                // .GroupBy(c => c.id).Select(g => g.First())
                .ToDictionary(
                k => k.id,
                v =>
                {
                    return toProcess.Count(c =>
                    {
                        var overlapStart = (c.from < v.from && c.to > v.from);
                        var overlapEnd = (c.to > v.to && c.from < v.to);
                        var inside = (c.from >= v.from && c.to <= v.to);
                        return overlapStart || overlapEnd || inside;
                    });
                });
                conflictingContainers = containerCollisions.Keys
                    .Select(i => toProcess.First(c => c.id == i))
                    .Where(k =>
                    {
                        return k.max > 0 && containerCollisions[k.id] > k.max;
                    })
                    .ToArray();

                Console.WriteLine(JsonConvert.SerializeObject(containerCollisions, Formatting.Indented));
                if (conflictingContainers.Any())
                {
                    Console.WriteLine(conflictingContainers.Length + " conflicts");
                    Console.WriteLine(JsonConvert.SerializeObject(conflictingContainers, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine("No conflicts");
                }
            }

            var delayDuration = delayTill - startTime;
            if (waitForDelay)
            {
                if (delayDuration.TotalMilliseconds > 0)
                {
                    Console.WriteLine($"Delayed {delayDuration}");
                }

                await Task.Delay(delayDuration);
            }

            return delayDuration;
        }

        public virtual void StopContainer(string containerId)
        {
            var container = this.dockerClient.Containers.InspectContainerAsync(containerId).Result;
            if (container.State.Running)
            {
                this.dockerClient.Containers.KillContainerAsync(containerId, new ContainerKillParameters()).GetAwaiter().GetResult();
            }
            this.dockerClient.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true, RemoveVolumes = true }).GetAwaiter().GetResult();
        }

        private const string DefaultNetwork = "bridge";
        private const string labelTestClientPid = "testClientPid";
        private string network = DefaultNetwork;
        public async Task DockerBridgeNetwork(bool enable, string preferredName)
        {
            bool currentlyEnabled = this.network != DefaultNetwork;
            if (enable == currentlyEnabled)
            {
                return;
            }

            string pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();

            if (enable && !currentlyEnabled)
            {
                var nets = await this.dockerClient.Networks.ListNetworksAsync();
                string suffix = string.Empty;
                int counter = 0;
                bool foundExistingNet;
                while (foundExistingNet = nets.Any(n => n.Name == preferredName + suffix
                && (!n.Labels.ContainsKey(labelTestClientPid) || n.Labels[labelTestClientPid] != pid)))
                {
                    suffix = $"_{counter++}";
                }

                if (!foundExistingNet)
                {
                    var net = await this.dockerClient.Networks.CreateNetworkAsync(new NetworksCreateParameters
                    {
                        Name = preferredName + suffix,
                        Attachable = true,
                        Labels = new Dictionary<string, string>
                        {
                            {labelTestClientPid, pid },
                        }
                    });
                    //netId = net.ID;
                    this.network = preferredName + suffix;
                }
                else
                {
                    var net = nets.Single(n => n.Name == preferredName + suffix);
                    //netId = net.ID;
                    this.network = net.Name;
                }
            }
        }

        public string GetDockerNetwork() => network;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.dockerClient.Dispose();
            }
        }
    }
}
