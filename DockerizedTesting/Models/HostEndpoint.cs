using System;
using System.Collections.Generic;
using System.Text;

namespace DockerizedTesting.Models
{
    /// <summary>
    /// Like IPEndpoint but mapping a hostname to port instead of and IP Address
    /// </summary>
    public class HostEndpoint : Tuple<string, int>
    {
        public HostEndpoint(string hostname, int port):base(hostname, port)
        {
        }

        public string Hostname => this.Item1;
        public int Port => this.Item2;

        public override string ToString() => $"{this.Hostname}:{this.Port}";
    }
}
