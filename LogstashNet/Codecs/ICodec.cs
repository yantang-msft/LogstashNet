using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Codecs
{
    internal interface ICodec
    {
        JObject Decode(string eventString);
        string Encode(JObject evt);
    }
}
