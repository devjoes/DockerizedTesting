using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DockerizedTesting.ImageProviders
{
    public interface IDockerfileLocator
    {
        FileInfo GetDockerfile(KeyValuePair<string, string> label);
    }
    public class DockerfileLocator : IDockerfileLocator
    {

        public FileInfo GetDockerfile(KeyValuePair<string, string> label) =>
            ignoreDockerFilesWhichAreCopiedToOutputOfOtherProjects(getDockerFiles()
                .Where(f => getLabels(f.FullName).Contains(label)).ToArray())
            ?? throw new ArgumentException($"Could not find Dockerfile with the label {label.Key} matching {label.Value}", nameof(label));

        private static FileInfo ignoreDockerFilesWhichAreCopiedToOutputOfOtherProjects(FileInfo[] dockerFiles)
        {
            var projectDirs = dockerFiles.Where(f => !f.FullName.Contains("bin"))
                .Select(f => f.Directory?.FullName + Path.DirectorySeparatorChar);
            return dockerFiles.LastOrDefault(f => projectDirs.Any(d => f.FullName.StartsWith(d)));
        }

        private static Dictionary<string, string> getLabels(string path) =>
            File.ReadAllLines(path)
                .Where(l => l.ToUpper().StartsWith("LABEL"))
                .Select(l => l.Substring(5).Trim())
                .Select(l => Regex.Matches($"{l}\n", "\"?([^\"]*)\"?=\"?([^\"]*)[\"\\s]"))
                .SelectMany(i => i.Cast<Match>())
                .GroupBy(r => r.Groups[1].Value.Trim('"')).Select(g => g.First())
                .ToDictionary(
                    k => k.Groups[1].Value,
                    v => v.Groups[2].Value);

        private static IEnumerable<FileInfo> getDockerFiles()
        {
            HashSet<FileInfo> found = new HashSet<FileInfo>();
            foreach (var file in ProjectLocator.FindSlnFiles()
                .Select(f => f.Directory)
                .Where(d => d != null)
                .SelectMany(d => d.GetFiles("Dockerfile", SearchOption.AllDirectories)))
            {
                if (found.Contains(file))
                {
                    continue;
                }
                found.Add(file);
                yield return file;
            }
        }
    }
}
