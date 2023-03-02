using System;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace DockerizedTesting.ImageProviders
{
    public class DockerHubImageProvider : IDockerImageProvider
    {
        private readonly string image;

        public DockerHubImageProvider(string image)
        {
            this.image = image;
        }

        public async Task<string> GetImage(IDockerClient dockerClient)
        {
            var splitImage = this.image.Split(':');

            ImageInspectResponse inspectExistingImageResponse = new ImageInspectResponse();
            try
            {
                inspectExistingImageResponse = await dockerClient.Images.InspectImageAsync(this.image);
            }
            catch (DockerImageNotFoundException)
            {
                inspectExistingImageResponse = null;
            }
            if (inspectExistingImageResponse == null)
            {
                await dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
                {
                    FromImage = splitImage.First(),
                    Tag = splitImage.Length == 1 ? "latest" : splitImage.Last()
                }, new AuthConfig(), new Progress<JSONMessage>());
            }
            return this.image;
        }
    }
}