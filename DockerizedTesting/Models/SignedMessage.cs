using System;
using System.Collections.Generic;
using System.Text;

namespace DockerizedTesting.Models
{
    public class SignedMessage<T>
    {
        public T Message { get; set; }
        public DateTime TimeSigned { get; set; }
        public string Signature { get; set; }
    }
}
