using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Codecs
{
    internal class PlainCodec : ICodec
    {
        public JObject Decode(string eventString)
        {
            return new JObject(
                 new JProperty("message", eventString));
        }

        public string Encode(JObject evt)
        {
            throw new NotImplementedException();
        }
    }
}
