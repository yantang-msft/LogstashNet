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
        private object _evaluator;

        public StdOutput(string condition = null)
        {
            if (!string.IsNullOrEmpty(condition))
            {
                _evaluator = Utilities.CompileCondition(condition);
            }
        }

        public override async Task TransferLogsAsync(List<JObject> messages)
        {
            foreach (var message in messages)
            {
                if (_evaluator != null && !Utilities.EvaluateCondition(_evaluator, message))
                {
                    continue;
                }

                Console.WriteLine("StdOutput: " + JsonConvert.SerializeObject(message));
            }
        }
    }
}
