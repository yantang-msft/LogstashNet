using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Outputs
{
    internal class StdOutput : OutputBase
    {
        public override async Task TransferLogsAsync(List<JObject> messages)
        {
            foreach (var message in messages)
            {
                Console.WriteLine("StdOutput: " + JsonConvert.SerializeObject(message));
            }
        }
    }
}
