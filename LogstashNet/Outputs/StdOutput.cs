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
        private string _condition;

        public StdOutput(string condition = null)
        {
            _condition = condition;
        }

        public override async Task TransferLogsAsync(List<JObject> messages)
        {
            foreach (var message in messages)
            {
                if (!string.IsNullOrEmpty(_condition) && !Utilities.EvaluateCondition(message, _condition))
                {
                    continue;
                }

                Console.WriteLine("StdOutput: " + JsonConvert.SerializeObject(message));
            }
        }
    }
}
