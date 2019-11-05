using System.Threading.Tasks;
using Docker.DotNet;

namespace DockerizedTesting.ImageProviders
{
    public interface IDockerImageProvider
    {
        Task<string> GetImage(IDockerClient dockerClient);
    }
}
