using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Docker.DotNet.Models;

namespace DockerizedTesting.Models
{
    public class ContainerStartException : Exception
    {
        public ContainerStartException(ContainerState state):base("Container failed to start. See exception properties for details." + state.Error)
        {
            Running = state.Running;
            Dead = state.Dead;
            FinishedAt = state.FinishedAt;
            Status = state.Health?.Status;
            Logs = state.Health?.Log?.Select(l => l.Output).ToArray();
            ExitCode = state.ExitCode;
            OOMKilled = state.OOMKilled;
            Restarting = state.Restarting;
        }

        public bool Running { get; }
        public bool Dead { get; }
        public string FinishedAt { get; }
        public string Status { get; }
        public string[] Logs { get; }
        public long ExitCode { get; }
        public bool OOMKilled { get; }
        public bool Restarting { get; }
    }
}
