using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Docker.DotNet.Models;

namespace DockerizedTesting.S3
{
    public class S3Fixture : BaseFixture<S3FixtureOptions>, IDisposable
    {
        public S3Fixture() : base("s3", 1)
        {
        }

        public Task Start()
        {
            return this.Start(new S3FixtureOptions());
        }

        public override Task Start(S3FixtureOptions options)
        {
            this.volumePath = options.VolumePath;
            if (!string.IsNullOrEmpty(options.VolumePath))
            {
                this.volumePath = options.VolumePath;
                if (!Directory.Exists(this.volumePath))
                {
                    throw new DirectoryNotFoundException($"Could not find: {this.volumePath}");
                }
            }
            return base.Start(options);
        }

        private string volumePath;
        private string tmpPath = null;

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int s3Port = 4569;

            var hostConfig = Utils.HostWithBoundPorts(ports, s3Port);
            if (!string.IsNullOrEmpty(this.volumePath))
            {
                hostConfig.Mounts = new List<Mount>
                {
                    new Mount
                    {
                        Source = this.volumePath,
                        Target = "/fakes3_root",
                        Type = "bind"
                    }
                };
            }

            return new CreateContainerParameters(
                new Config
                {
                    ExposedPorts = new Dictionary<string, EmptyStruct>() { { s3Port.ToString(), default } },
                })
            {
                HostConfig = hostConfig
            };
        }

        protected override async Task<bool> IsContainerRunning(int[] ports)
        {
            try
            {
                var s3Client = new AmazonS3Client(
                    new AnonymousAWSCredentials(),
                    new AmazonS3Config
                    {
                        ServiceURL = "http://127.0.0.1:" + ports.Single(),
                        ForcePathStyle = true,
                        Timeout = TimeSpan.FromSeconds(3),
                        ReadWriteTimeout = TimeSpan.FromSeconds(3),
                        MaxErrorRetry = 1
                    });
                var cts = new CancellationTokenSource();
                cts.CancelAfter(6000);
                const string bucketName = "foo";
                await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }, cts.Token);
                var buckets = await s3Client.ListBucketsAsync(cts.Token);
                await s3Client.DeleteBucketAsync(bucketName, cts.Token);
                return buckets.Buckets.Any(b => b.BucketName == bucketName);
            }
            catch
            {
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (this.tmpPath != null && Directory.Exists(this.tmpPath))
            {
                Directory.Delete(this.tmpPath, true);
            }
        }
    }
}
