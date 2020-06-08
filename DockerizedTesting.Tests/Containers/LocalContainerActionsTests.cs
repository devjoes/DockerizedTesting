using Docker.DotNet.Models;
using DockerizedTesting.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DockerizedTesting.Tests.Containers
{
    public class LocalContainerActionsTests
    {
        private readonly ITestOutputHelper output;
        private DateTime now;
        private ContainerListResponse[] runningContainers;

        // Most of the code in LocalContainerActions (or the general DockerizedTesting project in general)
        // Is tested implicitly by integration tests.

        public LocalContainerActionsTests(ITestOutputHelper output)
        {
            Console.SetOut(new ConsoleXunitAdapter(output));
            this.output = output;
            this.now = DateTime.UtcNow;

            ContainerListResponse buildFake(string name, string label)
            {
                return new ContainerListResponse
                {
                    ID = name.GetHashCode().ToString(),
                    Names = new List<string>() { name },
                    Labels = new Dictionary<string, string>
                    {
                        {LocalContainerActions.labelDockerizedTesting, label}
                    },
                    State = "running" //todo: test different values
                };
            }

            this.runningContainers = new ContainerListResponse[]
            {
                buildFake("A", $"1_{now.AddSeconds(-10)}_{now.AddSeconds(-5)}"),
                buildFake("B", $"2_{now.AddSeconds(-5)}_{now.AddSeconds(-3)}"),
                buildFake("C", $"2_{now.AddSeconds(3)}_{now.AddSeconds(4)}"),
                buildFake("D", $"1_{now.AddSeconds(8)}_{now.AddSeconds(10)}")
            };
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(1, 1, 1)]
        [InlineData(1, 3, 3)]
        [InlineData(2, 4, 3)]
        [InlineData(2, 3, 4)]
        [InlineData(3, 5, 4)]
        public async Task WaitTillContainerCanStart_StartsContainerImmediatlyWhenNoConflicts(int maxContainers, int beforeWindowSecs, int afterWindowSecs)
        {
            string id = "0"; //todo: test different values
            var schedulingOptions = new FixtureOptions.DelayedSchedulingOptions
            {
                MaxContainers = maxContainers,
                SchedulingWindowBefore = TimeSpan.FromSeconds(beforeWindowSecs),
                SchedulingWindowAfter = TimeSpan.FromSeconds(afterWindowSecs),
            };
            var delayedBy = await LocalContainerActions.WaitTillContainerCanStart(
                () => Task.FromResult((IList<ContainerListResponse>)this.runningContainers),
                id, schedulingOptions, CancellationToken.None, ()=> this.now, false);
            Assert.Equal(0, delayedBy.Ticks);
        }


        [Theory]
        [InlineData(1, 4, 1, 1)]  // After B before C
        [InlineData(1, 0, 4, 4)]  // After C before D
        [InlineData(2, 4, 4, 1)]  // After B before D - overlapping C
        [InlineData(3, 6, 4, 1)]  // After A before D - overlapping B and C
        [InlineData(1, 1, 10, 11)] // After D
        public async Task WaitTillContainerCanStart_DelaysWhenConflicted(int maxContainers, int beforeWindowSecs, int afterWindowSecs, int expectedDelaySecs)
        {
            string id = "123"; //todo: test different values
            var schedulingOptions = new FixtureOptions.DelayedSchedulingOptions
            {
                MaxContainers = maxContainers,
                SchedulingWindowBefore = TimeSpan.FromSeconds(beforeWindowSecs),
                SchedulingWindowAfter = TimeSpan.FromSeconds(afterWindowSecs),
            };
            var delayedBy = await LocalContainerActions.WaitTillContainerCanStart(
                () => Task.FromResult((IList<ContainerListResponse>)this.runningContainers),
                id, schedulingOptions, CancellationToken.None, () => this.now, false);
            Assert.Equal(expectedDelaySecs, delayedBy.TotalSeconds);
        }

        [Fact]
        public async Task WaitTillContainerCanStart_DelaysEvenIfAddedContainerHasNoScheduling()
        {
            string id = "0"; //todo: test different values
            var delayedBy = await LocalContainerActions.WaitTillContainerCanStart(
                () => Task.FromResult((IList<ContainerListResponse>)this.runningContainers),
                id, new FixtureOptions.DelayedSchedulingOptions(), CancellationToken.None,
                () => this.now.AddSeconds(-6), false);
            Assert.Equal(2, delayedBy.TotalSeconds);
        }

        [Fact]
        public async Task WaitTillContainerCanStart_DelaysEvenIfExistingContainersHaveNoScheduling()
        {
            string id = "0"; //todo: test different values
            var containersWithoutSchedule = Enumerable.Range(0, 5)
                .Select(i => new ContainerListResponse
                {
                    ID = "foo"+i,
                    State = "created", //todo: test different values
                    Names = new[] { i.ToString() },
                    Labels = new Dictionary<string, string>
                {
                    {LocalContainerActions.labelDockerizedTesting,$"0_{now}_{now}"}
                }
                }).ToArray();
            var schedulingOptions = new FixtureOptions.DelayedSchedulingOptions
            {
                MaxContainers = 2,
                SchedulingWindowBefore = TimeSpan.FromSeconds(3),
                SchedulingWindowAfter = TimeSpan.FromSeconds(3),
            };
            var delayedBy = await LocalContainerActions.WaitTillContainerCanStart(
                () => Task.FromResult((IList<ContainerListResponse>)containersWithoutSchedule), 
                id, schedulingOptions, CancellationToken.None, () => this.now, false);
            Assert.Equal(4, delayedBy.TotalSeconds);
        }
    }
}
