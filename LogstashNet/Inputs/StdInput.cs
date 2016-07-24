using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Inputs
{
    internal class StdInput : InputBase
    {
        public StdInput(ConcurrentQueue<JObject> messageQueue, string codec, string type)
            : base(messageQueue, codec, type)
        {
            Task.Run(() => StartListening());
        }

        private void StartListening()
        {
            while (true)
            {
                string line = Console.ReadLine();

                base.PushEvent(line);
            }
        }
    }
}
