using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Codecs
{
    internal class JsonCodec : ICodec
    {
        private static PlainCodec plainCodec = new PlainCodec();

        public JObject Decode(string eventString)
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(eventString);
            }
            catch (Exception e)
            {
                Utilities.WriteError(e.ToString());

                // Fall back to plain text codec if failed to deserialize to json object
                var json = plainCodec.Decode(eventString);
                json.AddTag("_jsonparsefailure");

                return json;
            }
        }

        public string Encode(JObject evt)
        {
            throw new NotImplementedException();
        }
    }
}
