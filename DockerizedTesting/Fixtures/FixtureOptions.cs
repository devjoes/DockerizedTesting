using DockerizedTesting.Containers;
using DockerizedTesting.ImageProviders;
using System;

namespace DockerizedTesting
{
    public abstract class FixtureOptions
    {
        /// <summary>
        /// Delay between calls to IsContainerRunning to check if the container is live
        /// </summary>
        public virtual int DelayMs { get; set; } = 750;
        /// <summary>
        /// Number of times to call IsContainerRunning before aborting
        /// </summary>
        public virtual int MaxRetries { get; set; } = 20;

        /// <summary>
        /// Constrains how many containers can be started within a period of time. 
        /// Useful if you receiver timeouts/OOM from the docker API
        /// </summary>
        public virtual DelayedSchedulingOptions DelayedScheduling { get; set; } = new DelayedSchedulingOptions();

        /// <summary>
        /// Returns a docker image to run
        /// </summary>
        public abstract IDockerImageProvider ImageProvider { get; }

        /// <summary>
        /// This is the amount of time to wait for a container to be created (download image etc)
        /// if 0 then GlobalConfig.DefaultCreationTimeoutMs
        /// </summary>
        public int CreationTimeoutMs { get; set; }

        /// <summary>
        /// The container will not be created or started within SchedulingWindowBefore + Now + SchedulingWindowAfter
        /// of MaxContainers. Equally other containers will not be created/started if they would break this rule.
        /// </summary>
        public class DelayedSchedulingOptions
        {
            public virtual TimeSpan SchedulingWindowBefore { get; set; }
            public virtual TimeSpan SchedulingWindowAfter { get; set; }
            public virtual int MaxContainers { get; set; }

            public virtual string GetLabel(string unique)
            {
                var now = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second);
                var start = now.AddSeconds(0 - this.SchedulingWindowBefore.TotalSeconds);
                var end = now.AddSeconds(this.SchedulingWindowAfter.TotalSeconds);
                return $"{this.MaxContainers}_{start}_{end}_{unique}";
            }
        }
    }
}