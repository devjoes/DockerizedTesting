using System;
using System.Collections.Generic;
using System.Text;
using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.MsSql
{
    public class MsSqlFixtureOptions : FixtureOptions
    {
        // This is quite a beefy container so we say it can't run withing 10 seconds of more than 3 other containers running
        public override DelayedSchedulingOptions DelayedScheduling =>
            new DelayedSchedulingOptions
            {
                MaxContainers = 3,
                SchedulingWindowBefore = TimeSpan.FromSeconds(10),
                SchedulingWindowAfter = TimeSpan.FromSeconds(10)
            };

        public override IDockerImageProvider ImageProvider { get; } = new DockerRepoImageProvider("mcr.microsoft.com/mssql/server:latest");
        public string SaPassword { get; set; } = "D0cK3rIz3d_T3sting!!";
        public bool AcceptEula { get; set; } = true;
        public MsSqlProduct Product { get; set; } = MsSqlProduct.Developer;

        public override int DelayMs => 2000;

        public override int MaxRetries => 30;
    }
}
