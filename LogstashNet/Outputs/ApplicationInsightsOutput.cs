using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Outputs
{
    internal class ApplicationInsightsOutput : OutputBase
    {
        public class AITraceConfig
        {
            private object _evaluator;

            public string Condition { get; set; }
            public object Evaluator
            {
                get
                {
                    if (_evaluator != null)
                    {
                        return _evaluator;
                    }

                    if (!string.IsNullOrEmpty(Condition))
                    {
                        _evaluator = Utilities.CompileCondition(Condition);
                    }
                    return _evaluator;
                }
            }
        }

        public class AIMetricConfig
        {
            private object _evaluator;
            public string Condition { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public object Evaluator
            {
                get
                {
                    if (_evaluator != null)
                    {
                        return _evaluator;
                    }

                    if (!string.IsNullOrEmpty(Condition))
                    {
                        _evaluator = Utilities.CompileCondition(Condition);
                    }
                    return _evaluator;
                }
            }
        }

        private readonly TelemetryClient _aiClient;
        private readonly AITraceConfig _traceConfig;
        private readonly AIMetricConfig _metricConfig;

        public ApplicationInsightsOutput(string ikey, AITraceConfig traceConfig, AIMetricConfig metricConfig)
        {
            _aiClient = new TelemetryClient();
            _aiClient.Context.InstrumentationKey = ikey;

            _traceConfig = traceConfig;
            _metricConfig = metricConfig;
        }

        public override async Task TransferLogsAsync(List<JObject> messages)
        {
            foreach(var message in messages)
            {
                TrackTrace(message);
                TraceMetric(message);
            }
        }

        private void TrackTrace(JObject message)
        {
            if (_traceConfig != null)
            {
                if (string.IsNullOrEmpty(_traceConfig.Condition) || Utilities.EvaluateCondition(_traceConfig.Evaluator, message))
                {
                    var traceMessage = message["Message"].ToString();
                    var traceTelemetry = new TraceTelemetry()
                    {
                        SeverityLevel = EventLevelToTraceLevel(message["Level"].ToString()),
                        Timestamp = DateTimeOffset.Now,
                        Message = string.IsNullOrEmpty(traceMessage) ? "Dummy message. It's a required field of AI." : traceMessage
                    };

                    var payloadNames = message["PayloadNames"] as JArray;
                    var payload = message["Payload"] as JArray;

                    if (payload != null && payloadNames != null)
                    {
                        for (int i = 0; i < Math.Min(payload.Count, payloadNames.Count); i++)
                        {
                            traceTelemetry.Properties.Add(payloadNames[i].ToString(), payload[i].ToString());
                        }
                    }

                    _aiClient.TrackTrace(traceTelemetry);
                }
            }
        }

        private void TraceMetric(JObject message)
        {
            if (_metricConfig != null)
            {
                if (string.IsNullOrEmpty(_metricConfig.Condition) || Utilities.EvaluateCondition(_metricConfig.Evaluator, message))
                {
                    string metricValue = null;
                    if (!message.TryExpandPropertyByPath(_metricConfig.Value, out metricValue))
                    {
                        Utilities.WriteError("Failed to get metric value from event by property path " + _metricConfig.Value);
                        return;
                    };

                    double value;
                    if (!double.TryParse(metricValue, out value))
                    {
                        Utilities.WriteError("Metric value must be double type. While the indicated value is " + metricValue);
                        return;
                    }

                    MetricTelemetry metric = new MetricTelemetry()
                    {
                        Timestamp = DateTimeOffset.Now,
                        Name = _metricConfig.Name ?? message["EventName"].ToString(),
                        Value = value
                    };
                    metric.Context.Operation.SyntheticSource = "LogstashNetDemo";

                    _aiClient.TrackMetric(metric);
                }
            }
        }

        private SeverityLevel? EventLevelToTraceLevel(string eventLevel)
        {
            switch (eventLevel)
            {
                case "1": return SeverityLevel.Critical;
                case "2": return SeverityLevel.Error;
                case "3": return SeverityLevel.Warning;
                case "4": return SeverityLevel.Information;
                case "5": return SeverityLevel.Verbose;
                default:  return null;
            }
        }
    }
}
