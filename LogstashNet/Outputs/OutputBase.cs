using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Outputs
{
    internal abstract class OutputBase
    {
        public abstract Task TransferLogsAsync(List<JObject> messages);
    }
}
