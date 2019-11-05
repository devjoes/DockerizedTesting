using System;
using System.Collections.Generic;
using System.Text;
using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.MsSql
{
    public class MsSqlFixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerHubImageProvider("mcr.microsoft.com/mssql/server:latest");
        public string SaPassword { get; set; } = "D0cK3rIz3d_T3sting!!";
        public bool AcceptEula { get; set; } = true;
        public MsSqlProduct Product { get; set; } = MsSqlProduct.Developer;

        public override int DelayMs => 2000;
    }
}
