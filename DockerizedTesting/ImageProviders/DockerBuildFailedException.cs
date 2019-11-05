using System;

namespace DockerizedTesting.ImageProviders
{
    public class DockerBuildFailedException : Exception
    {
        public DockerBuildFailedException(Exception innerEx): base("Build failed: " +innerEx.Message, innerEx)
        {
        }

        public DockerBuildFailedException(string errors) : base("Build failed")
        {
            this.BuildErrors = errors;
        }

        public string BuildErrors { get; set; }
    }
}