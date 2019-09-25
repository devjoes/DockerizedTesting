namespace DockerizedTesting
{
    public class FixtureOptions
    {
        public virtual int DelayMs { get; set; } = 750;
        public virtual int MaxRetries { get; set; } = 20;
        public virtual IContainerHost ContainerHost { get; } = DockerizedTesting.ContainerHost.Instance;
    }
}