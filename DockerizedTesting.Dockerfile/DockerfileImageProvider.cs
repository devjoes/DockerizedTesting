using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Docker.DotNet;

namespace DockerizedTesting.Dockerfile
{
    /// <summary>
    /// Builds a dockerized .NET project and provides the docker image which can then be used in integration tests.
    /// </summary>
    public class DockerfileImageProvider : IDockerImageProvider
    {
        private readonly FileInfo projectFile;
        private string tag;

        /// <summary>
        /// Builds a dockerized .NET project and provides the docker image which can then be used in integration tests.
        /// </summary>
        /// <param name="name">The name of the project files (without extension) or the assembly name (if different) or the root namespace.
        /// Use like this: DockerfileImageProvider(nameof(YourProject))</param>
        /// <param name="projectLocator"></param>
        public DockerfileImageProvider(string name, IProjectLocator projectLocator = null) :
            this((projectLocator ?? new ProjectLocator()).GetDockerProject(name))
        { }

        /// <summary>
        /// Builds a dockerized .NET project and provides the docker image which can then be used in integration tests.
        /// </summary>
        /// <param name="projectFile">The project file to build</param>
        public DockerfileImageProvider(FileInfo projectFile)
        {
            this.projectFile = projectFile;
        }
        
        public Task<string> GetImage(IDockerClient dockerClient)
        {
            if (this.tag == null)
            {
                this.tag = this.buildDockerProject(this.projectFile);
            }

            return Task.FromResult(this.tag);
        }

        private int getEpoch() =>(int) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

        private string buildDockerProject(FileInfo fileInfo)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo), "Could not find project (or project is does not support docker)");
            }
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Could not find csproj file", fileInfo.FullName);
            }

            var tag = Regex.Replace(fileInfo.Name.ToLower()
                              .Replace("csproj", string.Empty).Replace("vbproj", string.Empty)
                          , "[^a-z0-9]", string.Empty) + "_" + "dockerized_testing_" + getEpoch();
            var processStartInfo = new ProcessStartInfo("dotnet", $"build {fileInfo.FullName} -target:ContainerBuild -p:DockerDefaultTag={tag}");
            processStartInfo.RedirectStandardError = true;
            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new DockerProjectBuildFailedException(process.StandardError.ReadToEnd());
            }

            return tag;
        }


    }
}
