using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogstashNet.Codecs;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Inputs
{
    internal abstract class InputBase
    {
        private ConcurrentQueue<JObject> _messageQueue;
        private ICodec _codec;
        private string _type;

        public InputBase(ConcurrentQueue<JObject> messageQueue, string codecString = null, string type = null)
        {
            _messageQueue = messageQueue;
            _type = type;

            switch (codecString)
            {
                case "json":
                    _codec = new JsonCodec();
                    break;
                default:
                    _codec = new PlainCodec();
                    break;
            }
        }

        protected void PushEvent(string eventString)
        {
            var evt = _codec.Decode(eventString);

            if (!string.IsNullOrEmpty(_type))
            {
                evt["type"] = _type;
            }

            _messageQueue.Enqueue(evt);
        }
    }
}
