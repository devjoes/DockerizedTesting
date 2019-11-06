using System;

namespace DockerizedTesting.ImageProviders
{
    public class DockerBuildFailedException : Exception
    {
        public DockerBuildFailedException(Exception innerEx): base("Build failed, see inner exception for details: " + innerEx.Message, innerEx)
        {
        }

        public DockerBuildFailedException(string errors) : base("Build failed, see BuildErrors for details")
        {
            this.BuildErrors = errors;
        }

        public string BuildErrors { get; set; }
    }
}