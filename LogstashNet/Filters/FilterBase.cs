using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Filters
{
    internal abstract class FilterBase
    {
        public abstract void Apply(JObject evt);
    }
}
