using System;
using System.Collections.Generic;
using System.Text;
using DockerizedTesting.ImageProviders;

namespace DockerizedTesting.S3
{
    public class S3FixtureOptions : FixtureOptions
    {
        public override IDockerImageProvider ImageProvider { get; } = new DockerHubImageProvider("lphoward/fake-s3:latest");
        public string VolumePath { get; set; }
    }
}
