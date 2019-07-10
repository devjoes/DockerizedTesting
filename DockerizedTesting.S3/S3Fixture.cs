﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
            this.dockerImage = options.Image;
            this.volumePath = options.VolumePath;
            if (string.IsNullOrEmpty(options.VolumePath))
            {
                this.tmpPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(this.tmpPath);
                this.volumePath = this.tmpPath;
            }
            else
            {
                this.volumePath = options.VolumePath;
                if (!Directory.Exists(this.volumePath))
                {
                    throw new DirectoryNotFoundException($"Could not find: {this.volumePath}");
                }
            }
            return base.Start(options);
        }

        private string dockerImage;
        private string volumePath;
        private string tmpPath = null;

        protected override CreateContainerParameters GetContainerParameters(int[] ports)
        {
            const int s3Port = 4569;

            var hostConfig = Utils.HostWithBoundPorts(ports, s3Port);
            hostConfig.Mounts = new List<Mount>
            {
                new Mount
                {
                    Source = this.volumePath,
                    Target = "/fakes3_root",
                    Type = "bind"
                }
            };
            return new CreateContainerParameters(
                new Config
                {
                    Image = this.dockerImage,
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
                var s3Client = new AmazonS3Client(new AmazonS3Config
                {
                    ServiceURL = "http://127.0.0.1:" + ports.Single(),
                    ForcePathStyle = true
                });
                const string bucketName = "foo";
                await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
                var buckets = await s3Client.ListBucketsAsync();
                await s3Client.DeleteBucketAsync(bucketName);
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
