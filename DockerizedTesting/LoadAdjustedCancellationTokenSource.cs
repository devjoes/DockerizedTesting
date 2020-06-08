// This was an attempt to fix the problem where lots of containers start at once and the api times out.
// It doesn't accound for a demanding container starting after lots of small containers (and needs tidying up)
// This is now fixed by FixtureOptions.DelayedSchedulingOptions - however it would have been nice to fix it automatically.
//TODO: Delete this and related code once problem is confirmed fixed.

//using Docker.DotNet;
//using Docker.DotNet.Models;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace DockerizedTesting
//{
//    public class LoadAdjustedCancellationTokenSource
//    {
//        private readonly IMonitorContainers monitorContainers;

//        public TimeSpan UnadjustedLifetime { get; }

//        private double threshold;
//        private int additionalWaitMs;
//        public DateTime StartTime { get; }

//        private CancellationTokenSource cancellationTokenSource;
//        private readonly List<double> averageUsagePerSample;
//        private double latestWaitRatio;

//        public TokenWithStats Token { get; }

//        public LoadAdjustedCancellationTokenSource(DockerClient dockerClient, TimeSpan unadjustedLifetime)
//            : this(new MonitorContainers(dockerClient), unadjustedLifetime, 80)
//        {

//        }

//        public LoadAdjustedCancellationTokenSource(DockerClient dockerClient, TimeSpan unadjustedLifetime, double threshold)
//            : this(new MonitorContainers(dockerClient), unadjustedLifetime, threshold)
//        {

//        }
//        public LoadAdjustedCancellationTokenSource(IMonitorContainers monitorContainers, TimeSpan unadjustedLifetime, double threshold)
//        {
//            this.monitorContainers = monitorContainers;
//            this.UnadjustedLifetime = unadjustedLifetime;
//            this.threshold = threshold;
//            this.additionalWaitMs = 0;
//            this.StartTime = DateTime.UtcNow;
//            this.cancellationTokenSource = new CancellationTokenSource();
//            this.averageUsagePerSample = new List<double>();
//            this.Token = new TokenWithStats(cancellationTokenSource.Token,
//                () => this.averageUsagePerSample.ToArray(),
//                () => this.additionalWaitMs);

//            var captureWindow = Stopwatch.StartNew();
//            Task.Run(() => this.MonitorUsage(captureWindow));
//            Task.Run(() => this.CancelAfterDelay(captureWindow));
//        }

//        public void Cancel() => this.cancellationTokenSource.Cancel();
//        public void Cancel(bool throwOnFirstException) => this.cancellationTokenSource.Cancel(throwOnFirstException);


//        private async Task CancelAfterDelay(Stopwatch captureWindow)
//        {
//            var token = this.cancellationTokenSource.Token;
//            await Task.Delay(this.UnadjustedLifetime, token);
//            var additionalMs = this.additionalWaitMs;

//            // If we stopped part way through a capture window of n secs then we could be missing up to n secs
//            // Of additional wait time. So we take the remaining capture window time and multiply it by the last
//            // known wait ratio. If the utilisation is below the threshold then this will be 0.
//            var remainingMs = (int)(captureWindow.ElapsedMilliseconds * this.latestWaitRatio);

//            // We don't account for load during this second wait 
//            // (otherwise we could theoretically end up in a kind of Zeno's arrow situation and never finish)
//            await Task.Delay(additionalMs + remainingMs, token);

//            if (!token.IsCancellationRequested)
//            {
//                this.cancellationTokenSource.Cancel();
//            }
//        }

//        private async Task MonitorUsage(Stopwatch captureWindow)
//        {
//            const int sampleFrequencyMs = 1000; //todo
//            var token = this.cancellationTokenSource.Token;
//            var start = DateTime.UtcNow; //TODO: replace this with captureWindow
//            var tStart = this.monitorContainers.Start(token, start);
//            while (!token.IsCancellationRequested)
//            {
//                await Task.Delay(sampleFrequencyMs, token);
//                if (token.IsCancellationRequested)
//                {
//                    break;
//                }
//                var usage = GetCurrentWindowUsage(sampleFrequencyMs, start);
//                if (usage.Any())
//                {
//                    this.latestWaitRatio = 0;
//                    var averageUsage = usage.Average();
//                    this.averageUsagePerSample.Add(averageUsage);
//                    if (averageUsage > threshold)
//                    {
//                        this.latestWaitRatio = (averageUsage - threshold) / (100 - threshold);
//                        this.additionalWaitMs += (int)(this.latestWaitRatio * captureWindow.ElapsedMilliseconds);
//                    }
//                    captureWindow.Restart();
//                }
//            }
//            captureWindow.Stop();
//            await tStart;
//        }

//        private double[] GetCurrentWindowUsage(int sampleFrequencyMs, DateTime startTime)
//        {
//            var results = new List<double>();
//            foreach (var c in this.monitorContainers.MonitoredContainers.Values)
//            {
//                bool itemsLeftToProcessInWindow;
//                do
//                {
//                    itemsLeftToProcessInWindow = false;
//                    double msSinceStart = (DateTime.UtcNow - startTime).TotalMilliseconds;
//                    if (c.Usage.TryPop(out var latest))
//                    {
//                        itemsLeftToProcessInWindow = latest != default &&
//                            latest.ranMsAfterStart >= msSinceStart - sampleFrequencyMs;
//                        if (itemsLeftToProcessInWindow)
//                        {
//                            results.Add(latest.usage);
//                        }
//                    }
//                } while (itemsLeftToProcessInWindow);
//            }
//            return results.ToArray();
//        }
//    }

//    public class TokenWithStats
//    {
//        private readonly Func<double[]> getUsage;
//        private readonly Func<int> getAdditionalMs;

//        public TokenWithStats(CancellationToken token, Func<double[]> getUsage, Func<int> getAdditionalMs)
//        {
//            this.Token = token;
//            this.getUsage = getUsage;
//            this.getAdditionalMs = getAdditionalMs;
//        }

//        public CancellationToken Token { get; }
//        public double[] Usage
//        {
//            get => this.getUsage();
//        }
//        public int AdditionalMs
//        {
//            get => this.getAdditionalMs();
//        }
//    }

//    public interface IMonitorContainers
//    {
//        Dictionary<string, ContainerCpuMonitor> MonitoredContainers { get; }
//        Task Start(CancellationToken cancellationToken, DateTime startTime);
//    }

//    public class MonitorContainers : IMonitorContainers
//    {
//        private readonly DockerClient dockerClient;
//        private DateTime startTime;

//        public MonitorContainers(DockerClient dockerClient)
//        {
//            this.MonitoredContainers = new Dictionary<string, ContainerCpuMonitor>();
//            this.dockerClient = dockerClient;
//        }

//        public Dictionary<string, ContainerCpuMonitor> MonitoredContainers { get; private set; }

//        public async Task Start(CancellationToken cancellationToken, DateTime startTime)
//        {
//            this.startTime = startTime;
//            while (!cancellationToken.IsCancellationRequested)
//            {
//                await this.updateMonitoredContainers(cancellationToken);
//                await Task.Delay(1000, cancellationToken);
//            }
//        }

//        private async Task updateMonitoredContainers(CancellationToken cancellationToken)
//        {
//            var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters());

//            var toAdd = containers.Where(c => !MonitoredContainers.ContainsKey(c.ID))
//                .ToDictionary(c => c.ID, c =>
//                {
//                    var monitor = new ContainerCpuMonitor(this.startTime);
//                    dockerClient.Containers.GetContainerStatsAsync(c.ID, new ContainerStatsParameters(), monitor, cancellationToken);
//                    return monitor;
//                });
//            foreach (var id in MonitoredContainers.Keys.Where(id => !containers.Any(c => c.ID == id)).ToArray())
//            {
//                MonitoredContainers[id].Stopped = true;
//            }
//            foreach (var id in toAdd.Keys)
//            {
//                // We shouldn't need to do any locking here since we only ever add items and always enumerate by key
//                MonitoredContainers.Add(id, toAdd[id]);
//            }
//        }
//    }

//    public class ContainerCpuMonitor : IProgress<ContainerStatsResponse>
//    {
//        public const int MaxStatBufferSize = 10000;
//        public ContainerCpuMonitor()
//        {
//            this.start = DateTime.UtcNow;
//            this.sample = addToStack;
//        }
//        public ContainerCpuMonitor(DateTime start)
//        {
//            this.start = start;
//            this.sample = addToStack;
//        }
//        public ContainerCpuMonitor(Action<double, double> sample, DateTime start)
//        {
//            this.start = start;
//            this.sample = sample ?? addToStack;
//        }

//        public virtual ConcurrentStack<(double ranMsAfterStart, double usage)> Usage { get; private set; } = new ConcurrentStack<(double ranMsAfterStart, double usage)>();
//        private void addToStack(double usage, double ranMsAfterStart)
//        {
//            if (this.Usage.Count >= MaxStatBufferSize)
//            {
//                var newestHalf = this.Usage.Take(MaxStatBufferSize / 2).ToArray().Reverse().ToArray();
//                this.Usage = new ConcurrentStack<(double ranMsAfterStart, double usage)>(newestHalf);
//            }
//            this.Usage.Push((ranMsAfterStart, usage));
//        }
//        private ulong lastCpu = 0;
//        private ulong lastSys = 0;
//        private bool firstReport = true;
//        private readonly DateTime start;
//        private readonly Action<double, double> sample;
//        public bool Stopped { get; set; }

//        public void Report(ContainerStatsResponse value)
//        {
//            if (Stopped || value?.CPUStats?.CPUUsage?.PercpuUsage == null)
//            {
//                return;
//            }
//            if (firstReport)
//            {
//                firstReport = false;
//                lastCpu = value.CPUStats.CPUUsage.TotalUsage;
//                lastSys = value.CPUStats.SystemUsage;

//                return;
//            }

//            var curCpu = value.CPUStats.CPUUsage.TotalUsage;
//            var curSys = value.CPUStats.SystemUsage;
//            var cpuDiff = curCpu - lastCpu;
//            var sysDiff = curSys - lastSys;

//            if (sysDiff > 0)
//            {
//                var percentageUsage = ((double)cpuDiff / sysDiff) * 100 * value.CPUStats.CPUUsage.PercpuUsage.Count;
//                var now = DateTime.UtcNow;
//                if (percentageUsage >= 0 && percentageUsage <= 100)
//                {
//                    // todo: fix this
//                    // in the tests there is a concurrency issue where we sometimes have results arrive out of order
//                    // cos they are cumulative and unsigen this causes odd behaviour. IRL this data comes from docker and is ok
//                    //[InlineData(new[] { 1d, 1d, 1d }, 50, 5000, 5000)]
//                    sample(percentageUsage, (now - start).TotalMilliseconds);
//                }

//            }

//            lastCpu = curCpu;
//            lastSys = curSys;
//        }
//    }
//}
