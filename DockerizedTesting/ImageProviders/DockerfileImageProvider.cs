using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;

namespace DockerizedTesting.ImageProviders
{
    public class DockerfileImageProvider : IDockerImageProvider
    {
        private readonly string dockerfilePath;
        private readonly string dockerContextPath;
        private readonly ImageBuildParameters buildParameters;

        /// <summary>
        /// Builds an image from a Dockerfile in the solution's folder with a matching "project" label.
        /// </summary>
        /// <param name="projectName">Value of the project label</param>
        /// <param name="contextRelativeToDockerfile">Directory to build the docker file from (normally "." or "..")</param>
        /// <param name="buildParameters">Optional parameters</param>
        /// <param name="locator">Locates Dockerfile</param>
        public DockerfileImageProvider(string projectName, string contextRelativeToDockerfile, ImageBuildParameters buildParameters = null, IDockerfileLocator locator = null)
        : this(new KeyValuePair<string, string>("project", projectName), contextRelativeToDockerfile, buildParameters, locator)
        {

        }

        /// <summary>
        /// Builds an image from a Dockerfile in the solution's folder with a matching label.
        /// </summary>
        /// <param name="label">Label to match</param>
        /// <param name="contextRelativeToDockerfile">Directory to build the docker file from (normally "." or "..")</param>
        /// <param name="buildParameters">Optional parameters</param>
        /// <param name="locator">Locates Dockerfile</param>
        public DockerfileImageProvider(KeyValuePair<string, string> label, string contextRelativeToDockerfile, ImageBuildParameters buildParameters = null, IDockerfileLocator locator = null)
        : this((locator ?? new DockerfileLocator()).GetDockerfile(label), contextRelativeToDockerfile, buildParameters)
        {

        }

        /// <summary>
        /// Builds an image from a Dockerfile. If possible don't use this approach as it can be brittle.
        /// </summary>
        /// <param name="dockerFile">Dockerfile to build</param>
        /// <param name="contextRelativeToDockerfile">Directory to build the docker file from (normally "." or "..")</param>
        /// <param name="buildParameters">Optional parameters</param>
        public DockerfileImageProvider(FileInfo dockerFile, string contextRelativeToDockerfile, ImageBuildParameters buildParameters = null)
        {
            this.buildParameters = buildParameters ??
                                   new ImageBuildParameters
                                   {
                                       Remove = true
                                   };
            this.dockerfilePath = dockerFile.FullName;
            if (!File.Exists(this.dockerfilePath))
            {
                throw new FileNotFoundException("Could not find Dockerfile", this.dockerfilePath);
            }

            this.dockerContextPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(this.dockerfilePath), contextRelativeToDockerfile));
            if (!Directory.Exists(this.dockerContextPath))
            {
                throw new FileNotFoundException("Could not find docker context", this.dockerContextPath);
            }
        }

        private static Stream createTarballForDockerfileDirectory(string directory, string[] extraFilesToInclude)
        {
            var tarball = new MemoryStream();
            var files = getFiles(directory);
            files = files.Concat(extraFilesToInclude).Distinct(); // For some reason .dockerignore contains Dockerfile
            var archive = new TarOutputStream(tarball)
            {
                IsStreamOwner = false
            };
            using (archive)
            {
                foreach (var file in files)
                {
                    string tarName = file.Substring(directory.Length).Replace('\\', '/').TrimStart('/');

                    var entry = TarEntry.CreateTarEntry(tarName);
                    using (var fileStream = File.OpenRead(file))
                    {
                        entry.Size = fileStream.Length;
                        archive.PutNextEntry(entry);

                        byte[] localBuffer = new byte[32 * 1024];
                        while (true)
                        {
                            int numRead = fileStream.Read(localBuffer, 0, localBuffer.Length);
                            if (numRead <= 0)
                                break;

                            archive.Write(localBuffer, 0, numRead);
                        }
                        archive.CloseEntry();
                    }
                }

                archive.Close();
            }

            tarball.Position = 0;
            return tarball;
        }

        private static IEnumerable<string> getFiles(string directory)
        {
            string escapedSeparator = Regex.Escape(Path.DirectorySeparatorChar.ToString());
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories).ToList();
            var rxs = files.Where(f => f.EndsWith(".dockerignore"))
                .SelectMany(f =>
                    File.ReadAllLines(f)
                        .Select(l => l.Replace('/', Path.DirectorySeparatorChar))
                        .Select(l =>
                            Regex.Escape(Path.Combine(Path.GetDirectoryName(f), l))
                                .Replace("\\*", "[^\n]*")
                                .Replace(escapedSeparator + "[^\n]*[^\n]*" + escapedSeparator,
                                    escapedSeparator + "[^\n]*")
                        ))
                .ToArray();
            string fileString = "\n" + string.Join("\n", files) + "\n";
            foreach (var pattern in rxs)
            {
                fileString = Regex.Replace(fileString, $"\n({pattern}\n)|({pattern}{escapedSeparator}.*\n)", "\n");
            }

            return fileString.Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);
        }

        private static string getHash(Stream str)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(str);
                str.Position = 0;
                return Convert.ToBase64String(hash)
                    .ToLower()
                    .Replace("+", string.Empty)
                    .Replace("/", string.Empty)
                    .Replace("=", string.Empty);
            }
        }

        public async Task<string> GetImage(IDockerClient dockerClient)
        {
            this.buildParameters.Dockerfile = this.dockerfilePath
                .Replace(this.dockerContextPath, string.Empty)
                .Replace(Path.DirectorySeparatorChar, '/');

            var tarball = createTarballForDockerfileDirectory(this.dockerContextPath, new[] { this.dockerfilePath });

            string tag = "dockerized_testing_" + Regex.Replace(this.dockerfilePath.ToLower(), "[^a-z0-9]", "_").Trim('_') + ":" +
                         getHash(tarball);
            this.buildParameters.Tags = new[] { tag };

            try
            {
                using (var result = await dockerClient.Images.BuildImageFromDockerfileAsync(tarball, this.buildParameters))
                {

                    using (var reader = new StreamReader(result))
                    {
                        string resultContent = reader.ReadToEnd();

                        var images = await dockerClient.Images.ListImagesAsync(new ImagesListParameters{MatchName = tag });
                        if (!images.Any())
                        {
                            throw new DockerBuildFailedException(resultContent);
                        }
                    }

                    return tag;
                }
            }
            catch (DockerApiException ex)
            {
                throw new DockerBuildFailedException(ex);
            }
        }

    }
}
