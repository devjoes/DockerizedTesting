using System.Threading.Tasks;
using Docker.DotNet;

namespace DockerizedTesting.ImageProviders
{
    public interface IDockerImageProvider
    {
        //TODO: This should ref a local image by hash (and pull it if it isnt available) then it can be pushed as a tar if docker is remote
        Task<string> GetImage(IDockerClient dockerClient);
    }
}
