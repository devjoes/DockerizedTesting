using Docker.DotNet.Models;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DockerizedTesting.Tests
{
    public class UsageAdjustedCancelationTokenSourceTests
    {
        [Fact]
        public async Task NoAdjustmentIfNothingRunning()
        {
            var monitorContainers = new Mock<IMonitorContainers>();
            monitorContainers.Setup(m => m.MonitoredContainers).Returns(new Dictionary<string, ContainerCpuMonitor>());
            var lifetime = TimeSpan.FromSeconds(5);
            
            var cts = new LoadAdjustedCancellationTokenSource(monitorContainers.Object, lifetime, 80);
            var token = cts.Token;
            var sw = Stopwatch.StartNew();
            while (!token.Token.IsCancellationRequested && sw.Elapsed < lifetime * 2)
            {
                await Task.Delay(1);
            }
            sw.Stop();

            Assert.Equal(DateTime.MinValue.Add(lifetime), DateTime.MinValue.AddTicks(sw.ElapsedTicks), new TimeSpan(0, 0, 0, 0, 100));
        }

        [Theory]
        [InlineData(2, 5000, 0, 500, 2000, 100, new[] { 0d, 0d })]
        [InlineData(2, 5000, 5000, 500, 2000, 100, new[] { 1d, 1d })]
        [InlineData(2, 5000, 0, 500, 2000, 100, new[] { 0.49d, 0.49d }, new[] { 0.49d, 0.49d })]
        [InlineData(3, 5000, 2500, 500, 2000, 100, new[] { 0.75d, 0.75d }, new[] { 0.75d, 0.75d }, new[] { 0.75d, 0.75d })]
        [InlineData(3, 10000, 5000, 500, 2000, 100, new[] { 0.5d, 1d }, new[] { 0.5d, 1 }, new[] { 0.5d, 1d })]
        [InlineData(3, 10000, 5000, 500, 1000, 10, new[] { 0.5d, 1d }, new[] { 0.5d, 1 }, new[] { 0.5d, 1d })]
        [InlineData(10, 10000, 5000, 500, 1000, 100, new[] { 0.75d,0.75d }, new[] { 0.5d, 1 }, new[] { 0.5d, 1d })]
        public async Task ExpirationIsBasedOnLoadAndThreshold(int containerCount, int lifetimeMs, int expectedAdditionalMs, int precisionMs, int frequencyMs, int delayMs, params double[][] loads)
        {
            const int threshold = 50;
            var additionalTime = TimeSpan.FromMilliseconds(expectedAdditionalMs);

            var monitorContainers = new Mock<IMonitorContainers>();
            mockMonitoredContainers(monitorContainers, containerCount, delayMs, frequencyMs, loads);
            //TODO: convert all percentages to floating points

            var lifetime = TimeSpan.FromMilliseconds(lifetimeMs);
            var cts = new LoadAdjustedCancellationTokenSource(monitorContainers.Object, lifetime, threshold);

            var token = cts.Token.Token;
            var sw = Stopwatch.StartNew();
            while (!token.IsCancellationRequested && sw.Elapsed < lifetime * 3)
            {
                await Task.Delay(1);
            }
            sw.Stop();

            Assert.Equal(DateTime.MinValue.Add(lifetime + additionalTime),
                DateTime.MinValue.AddTicks(sw.ElapsedTicks),
                TimeSpan.FromMilliseconds(precisionMs));
        }

        private static ContainerCpuMonitor[] mockMonitoredContainers(Mock<IMonitorContainers> monitorContainers, int containerCount, int delayMs, int frequencyMs, double[][] loads)
        {
            Assert.True(containerCount >= loads.Length);
            var cpuMonitors = loads.Select(l =>
            {
                var cpuMonitorMock = new Mock<ContainerCpuMonitor>() { CallBase = true };
                cpuMonitorMock.Setup(c => c.Usage).Returns(new ConcurrentStack<(double msSinceStart, double usage)>());
                var cpuMonitor = cpuMonitorMock.Object;
                emulateLoad(cpuMonitor, l[0], l[1], delayMs, frequencyMs);
                return cpuMonitor;
            }).ToArray();

            monitorContainers.Setup(m => m.MonitoredContainers).Returns(
                Enumerable.Range(0, containerCount).ToDictionary(
                    k => $"monitor{k}", 
                    i => cpuMonitors[i%cpuMonitors.Length]));
            return cpuMonitors;
        }

        private static Task emulateLoad(ContainerCpuMonitor monitor, double fromPercent, double toPercent, int delayMs, int frequencyMs)
            => Task.Run(() =>
{
    var sw = Stopwatch.StartNew();
    var step = (toPercent-fromPercent) / (frequencyMs / delayMs);
    ulong lastSys = 0;
    ulong lastCpu = 0;
    int counter = 0;
    while (!monitor.Stopped && sw.ElapsedMilliseconds < 30000)
    {
        var res = ContainerCpuMonitorTests.GenerateStatsResponse(1);
        res.CPUStats.SystemUsage = lastSys + 100;
        var percentageIncrease = 100 * step * counter++;
        res.CPUStats.CPUUsage.TotalUsage =(ulong)(lastCpu + (100 * fromPercent) + percentageIncrease);
        
        monitor.Report(res);

        lastSys = res.CPUStats.SystemUsage;
        lastCpu = res.CPUStats.CPUUsage.TotalUsage;
        Task.Delay(delayMs).Wait();
        if ((100 * fromPercent) + 100 * step * counter >= 100)
        {
            counter = 0;
        }
    }
});



        public class ContainerCpuMonitorTests
        {
            [Fact]
            public async Task ConstantUsage50PercCpu()
            {
                var usage = await this.useCpu(2, i => i + 1000, i => i + 4000);
                Assert.Equal(50, usage.Average());
            }


            [Fact]
            public async void CapacityOfStackIsLimited()
            {
                var trackContainerCpu = new ContainerCpuMonitor(DateTime.UtcNow);
                var statsResponse = GenerateStatsResponse(1);
                const int extra = 100;
                for (ulong i = 0; i <= ContainerCpuMonitor.MaxStatBufferSize + extra; i++)
                {
                    statsResponse.CPUStats.CPUUsage.TotalUsage = i * 5ul;
                    statsResponse.CPUStats.SystemUsage = i * 10ul;
                    trackContainerCpu.Report(statsResponse);
                    if (i == ContainerCpuMonitor.MaxStatBufferSize)
                    {
                        Assert.Equal(ContainerCpuMonitor.MaxStatBufferSize, trackContainerCpu.Usage.Count);
                    }
                }
                Assert.Equal(ContainerCpuMonitor.MaxStatBufferSize / 2 + extra, trackContainerCpu.Usage.Count);
            }

            [Fact]
            public async Task IncreaseTo99()
            {
                ulong counter = 0;
                // these are cumulative
                var usage = await this.useCpu(1,
                    i => i + counter++, // 0, 1, 2
                    i => i + 100 // 100, 200, 300
                    );
                Assert.Equal(1, usage.First()); // First percentage (0%) gets ignored because we cant calc the delta
                Assert.Equal(99, usage.Last());
                Assert.Equal(50, usage.Average());
            }

            private async Task<double[]> useCpu(int cores, Func<ulong, ulong> fCpu, Func<ulong, ulong> fSys)
            {
                List<double> usage = new List<double>();
                double lastMs = -1;
                var trackContainerCpu = new ContainerCpuMonitor((u, ms) =>
                {
                    usage.Add(u);
                    Assert.True(ms > lastMs);
                    lastMs = ms;
                }, DateTime.UtcNow);
                var statsResponse = GenerateStatsResponse(cores);

                for (int i = 0; i < 100; i++)
                {
                    statsResponse.CPUStats.CPUUsage.TotalUsage = fCpu(statsResponse.CPUStats.CPUUsage.TotalUsage);
                    statsResponse.CPUStats.SystemUsage = fSys(statsResponse.CPUStats.SystemUsage);
                    trackContainerCpu.Report(statsResponse);
                    await Task.Delay(10);
                }
                return usage.ToArray();
            }

            public static ContainerStatsResponse GenerateStatsResponse(int cores)
                => new Docker.DotNet.Models.ContainerStatsResponse
                {
                    CPUStats = new Docker.DotNet.Models.CPUStats
                    {
                        CPUUsage = new Docker.DotNet.Models.CPUUsage
                        {
                            PercpuUsage = new ulong[cores],
                            TotalUsage = 0,
                        },
                        SystemUsage = 0
                    }
                };
        }
    }
}
