using System;
using System.Collections.Generic;
using System.Text;

namespace DockerizedTesting.S3
{
    public class S3FixtureOptions : FixtureOptions
    {
        public string Image { get; set; } = "lphoward/fake-s3:latest";
        public string VolumePath { get; set; }
    }
}
