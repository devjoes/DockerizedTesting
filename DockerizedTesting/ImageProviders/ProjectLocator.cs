using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DockerizedTesting.ImageProviders
{
    public interface IProjectLocator
    {
        FileInfo GetDockerProject(string name);
    }

    public class ProjectLocator : IProjectLocator
    {
        private const string Project = "Project";
        private const string RootNamespace = "RootNamespace";
        private const string AssemblyName = "AssemblyName";
        private const string PropertyGroup = "PropertyGroup";
        private const string DockerDefaultTargetOs = "DockerDefaultTargetOS";

        public IEnumerable<(string path, string[] names)> GetProjectNamesFromSln(string slnPath) =>
            this.GetProjectNamesFromSln(slnPath, p => p.ContainsKey(DockerDefaultTargetOs));

        public IEnumerable<(string path, string[] names)> GetProjectNamesFromSln(string slnPath,
            Func<Dictionary<string, string>, bool> filter) =>
            this.getProjectPathsFromSln(slnPath)
                .Select(path => new {path, projectProps = this.extractProjectProperties(path)})
                .Where(p => filter(p.projectProps))
                .Select(i =>
                    (i.path, new[]
                    {
                        Path.GetFileNameWithoutExtension(i.path),
                        i.projectProps.ContainsKey(RootNamespace) ? i.projectProps[RootNamespace] : null,
                        i.projectProps.ContainsKey(AssemblyName) ? i.projectProps[AssemblyName] : null
                    }.Where(s => s != null).ToArray())
                );

        public static IEnumerable<FileInfo> FindSlnFiles()
        {
            var curDir = GetCurrentDir();
            return searchForSlnFiles(curDir);
        }

        public static DirectoryInfo GetCurrentDir()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return new DirectoryInfo(Path.GetDirectoryName(path) ?? throw new InvalidOperationException());
        }

        public FileInfo GetDockerProject(string name)
        {
            foreach (var sln in FindSlnFiles())
            {
                var project = this.GetProjectNamesFromSln(sln.FullName).SingleOrDefault(p => p.names.Contains(name));
                if (project != default)
                {
                    return new FileInfo(project.path);
                }
            }

            return null;
        }

        private Dictionary<string,string> extractProjectProperties(string path)
        {
            var proj = XDocument.Load(path);
            var elem = proj.Element(Project)?.Element(PropertyGroup);
            if (elem == null)
            {
                throw new InvalidOperationException("Invalid project xml");
            }
            return elem.Descendants().ToDictionary(k => k.Name.LocalName, v => v.Value);
        }

        private static readonly Regex RxExtractProjectPath = new Regex("\"[^\"]+\\.(csproj|vbproj)\"", RegexOptions.IgnoreCase);
        private IEnumerable<string> getProjectPathsFromSln(string slnPath) =>
            File.ReadAllLines(slnPath)
                .Where(l => RxExtractProjectPath.IsMatch(l))
                .Select(l => RxExtractProjectPath.Match(l).Captures[0].Value.Trim('"').Replace('\\', Path.DirectorySeparatorChar))
                .Select(f => Path.Combine(Path.GetDirectoryName(slnPath), f))
                .Where(File.Exists)
                .Distinct();
        

        private static IEnumerable<FileInfo> searchForSlnFiles(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles("*.sln").Concat(dir.Parent == null
                ? Array.Empty<FileInfo>()
                : searchForSlnFiles(dir.Parent)))
            {
                yield return file;
            }
        }
    }
}
