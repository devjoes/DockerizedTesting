using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using DockerizedTesting.ImageProviders;
using DockerizedTesting.Models;

namespace DockerizedTesting.Containers
{
//    public class RemoteContainerActions : IContainerActions
//    {
//        public RemoteContainerActions(IRemoteDockerConfig config)
//        {
//            this.client = new HttpClient { BaseAddress = config.EndPoint };
//            this.messageSigner = new MessageSigner(Utils.ReadPemKey(config.PublicKey));

//        }

//        public Task KillZombieContainersBoundToPorts(int[] ports)
//        {
//            throw new NotImplementedException();
//        }

//        public HostEndpoint[] ReservePorts(int count)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<string> StartContainer(CreateContainerParameters containerParams, IDockerImageProvider imageProvider, string containerName)
//        {
//            throw new NotImplementedException();
//        }

//        public void StopContainer(string containerId)
//        {
//            throw new NotImplementedException();
//        }
//    }
}
