using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogstashNet
{
    internal class ExtendableObject
    {
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }
    }

    [DataContract()]
    internal class Config : ExtendableObject
    {
        [DataMember]
        public Input Input { get; set; }
        [DataMember]
        public Output Output { get; set; }
    }

    [DataContract()]
    internal class Input : ExtendableObject
    {
        [DataMember]
        public EtwEventSource EtwEventSource { get; set; }
    }

    [DataContract()]
    internal class EtwEventSource : ExtendableObject
    {
        [DataMember]
        public List<string> providers { get; set; }
    }

    [DataContract()]
    internal class Output : ExtendableObject
    {
        [DataMember]
        public StdOut StdOut { get; set; }
    }

    [DataContract(Name = "stdout")]
    internal class StdOut: ExtendableObject
    {

    }
}
