using DockerizedTesting.Containers;
using DockerizedTesting.ImageProviders;

namespace DockerizedTesting
{
    public abstract class FixtureOptions
    {
        public virtual int DelayMs { get; set; } = 750;
        public virtual int MaxRetries { get; set; } = 20; //TODO: replace
        public abstract  IDockerImageProvider ImageProvider { get; }

        /// <summary>
        /// This is the amount of time to wait for a container to be created (download image etc)
        /// if 0 then GlobalConfig.DefaultCreationTimeoutMs
        /// </summary>
        public int CreationTimeoutMs { get; set; }
    }
}