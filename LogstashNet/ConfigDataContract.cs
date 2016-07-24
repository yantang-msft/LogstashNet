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
        public Filter Filter { get; set; }

        [DataMember]
        public Output Output { get; set; }
    }

    [DataContract()]
    internal class Input : ExtendableObject
    {
        [DataMember]
        public Stdin Stdin { get; set; }

        [DataMember]
        public EtwEventSource EtwEventSource { get; set; }
    }

    [DataContract()]
    internal class InputContractBase : ExtendableObject
    {
        [DataMember]
        public string codec { get; set; }

        [DataMember]
        public string type { get; set; }
    }

    [DataContract()]
    internal class Stdin : InputContractBase
    {

    }

    [DataContract()]
    internal class EtwEventSource : InputContractBase
    {
        [DataMember]
        public List<string> providers { get; set; }
    }

    [DataContract()]
    internal class Filter
    {
        [DataMember]
        public Grok Grok { get; set; }
    }

    [DataContract()]
    internal class Grok
    {
        [DataMember]
        public string Condition { get; set; }
        [DataMember]
        public List<string> Match { get; set; }
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
