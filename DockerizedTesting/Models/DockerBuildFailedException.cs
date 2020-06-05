using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DockerizedTesting.ImageProviders
{
    public class DockerBuildFailedException : Exception
    {
        public DockerBuildFailedException(Exception innerEx) : base("Build failed, see inner exception for details: " + innerEx.Message, innerEx)
        {
        }

        public DockerBuildFailedException(string errors) : base("Build failed, see BuildErrors for full details.\n"+TryDeserializeErrors(errors))
        {
            this.BuildErrors = errors;
        }

        internal static string TryDeserializeErrors(string errors)
        {
            string errorText = string.Join("\n", errors.Split('\r','\n').Select(l =>
            {
                try
                {
                    return JObject.Parse(l)["stream"].ToString();
                }
                catch { return l; }
            }));

            errorText = Regex.Replace(errorText, "\\n\\s*\\n", "\n");

            if (errorText.Length < 2000)
            {
                return errorText;
            }

            var start = errorText.Remove(1000);
            var end = errorText.Substring(errorText.Length - 1000);
            return $"...{start}\n...\n{end}...";
        }

        public string BuildErrors { get; set; }
    }
}