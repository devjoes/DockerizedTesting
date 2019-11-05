using System;

namespace DockerizedTesting.Dockerfile
{
    public class DockerProjectBuildFailedException : Exception
    {
        public DockerProjectBuildFailedException(string errors) : base("Build failed")
        {
            this.BuildErrors = errors;
        }

        public string BuildErrors { get; set; }
    }
}