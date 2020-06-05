using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerizedTesting.RemoteDocker.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class NetworkController:ControllerBase
    {
        private readonly ILogger<NetworkController> logger;

        public NetworkController(ILogger<NetworkController> logger)
        {
            this.logger = logger;
        }
    }
}
