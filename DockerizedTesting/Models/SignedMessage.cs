using Newtonsoft.Json;
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
        [JsonIgnore]
        public bool Valid { get; private set; }

        public void SetValid(bool v)
        {
            this.Valid = v;
        }
    }
}
