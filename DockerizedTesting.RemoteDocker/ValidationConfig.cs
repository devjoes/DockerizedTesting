using System;

namespace DockerizedTesting.RemoteDocker
{
    public class ValidationConfig
    {
        public string PrivateKey { get; set; }
        public TimeSpan MaxTimeDifference { get; set; } = TimeSpan.FromSeconds(5);
    }
}