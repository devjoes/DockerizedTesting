using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DockerizedTesting.RemoteDocker.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContainerController : ControllerBase
    {
        private readonly ILogger<ContainerController> logger;

        public ContainerController(ILogger<ContainerController> logger)
        {
            this.logger = logger;
        }

    }
}
