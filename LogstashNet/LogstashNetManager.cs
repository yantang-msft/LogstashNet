using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogstashNet.Filters;
using LogstashNet.Inputs;
using LogstashNet.Outputs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogstashNet
{
    public class LogstashNetManager
    {
        private ConcurrentQueue<JObject> _messageQueue = new ConcurrentQueue<JObject>();
        private List<InputBase> _inputPlugins = new List<InputBase>();
        private List<FilterBase> _filterPlugins = new List<FilterBase>();
        private List<OutputBase> _outputPlugins = new List<OutputBase>();
        private int _batchSize;
        private int _batchDelay;

        public LogstashNetManager(string configFilePath, int workerThreadNum = 8, int batchSize = 50, int batchDelay = 1000)
        {
            _batchSize = batchSize;
            _batchDelay = batchDelay;

            // Initialize plugins
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));
            InitializeInputPlugins(config);
            InitializeFilterPlugins(config);
            InitializedOutputPlugins(config);

            // TODO: Plugin extensibility

            // Start log forwarding
            // TODO: This output sequence is not ordered even for the same input source.
            for (int i = 0; i < workerThreadNum; i++)
            {
                Task.Run(() => ForwardLogs());
            }

            // TODO: handle corrupt and restart scenario
        }

        private void InitializeInputPlugins(Config config)
        {
            if (config.Input != null)
            {
                var stdinConfig = config.Input.Stdin;
                if (stdinConfig != null)
                {
                    _inputPlugins.Add(new StdInput(_messageQueue, stdinConfig.codec, stdinConfig.type));
                }

                var etwEventSourceConfig = config.Input.EtwEventSource;
                if (etwEventSourceConfig != null)
                {
                    _inputPlugins.Add(new EtwEventSourceInput(_messageQueue,
                        etwEventSourceConfig.providers, etwEventSourceConfig.codec, etwEventSourceConfig.type));
                }
            }
        }

        private void InitializeFilterPlugins(Config config)
        {
            if (config.Filter != null)
            {
                var grokConfig = config.Filter.Grok;
                if (grokConfig != null)
                {
                    if (grokConfig.Match.Count != 2)
                    {
                        throw new Exception("Grok filter's match property must be an array with 2 elements. The first element is the path of the property to match, the second element is the grok pattern.");
                    }

                    _filterPlugins.Add(new GrokFilter(grokConfig.Match, grokConfig.Condition));
                }
            }
        }

        private void InitializedOutputPlugins(Config config)
        {
            if (config.Output != null)
            {
                if (config.Output.StdOut != null)
                {
                    _outputPlugins.Add(new StdOutput(config.Output.StdOut.Condition));
                }

                if (config.Output.ApplicationInsights != null)
                {
                    var appInsightsConfig = config.Output.ApplicationInsights;
                    var traceConfig = appInsightsConfig.Trace == null ? null : new ApplicationInsightsOutput.AITraceConfig()
                    {
                        Condition = appInsightsConfig.Trace.Condition
                    };
                    var metricConfig = appInsightsConfig.Metric == null ? null : new ApplicationInsightsOutput.AIMetricConfig()
                    {
                        Condition = appInsightsConfig.Metric.Condition,
                        Name = appInsightsConfig.Metric.Name,
                        Value = appInsightsConfig.Metric.Value
                    };

                    _outputPlugins.Add(new ApplicationInsightsOutput(appInsightsConfig.InstrumentationKey, traceConfig, metricConfig));
                }
            }
        }

        private async void ForwardLogs()
        {
            // TODO: Is a shutdown function needed? If so, pass in an CancellationToken to terminate
            while (true)
            {
                var eventList = new List<JObject>();
                var endTime = DateTime.Now.AddMilliseconds(_batchDelay);

                JObject message = null;
                while (eventList.Count < _batchSize && _messageQueue.TryDequeue(out message))
                {
                    eventList.Add(message);
                }

                // Didn't fetch enough items, which means the input load is not high.
                if (eventList.Count < _batchSize)
                {
                    var currentTime = DateTime.Now;
                    if (endTime > currentTime)
                    {
                        await Task.Delay((endTime - currentTime).Milliseconds);
                    }
                }

                foreach (var evt in eventList)
                {
                    foreach (var filter in _filterPlugins)
                    {
                        filter.Apply(evt);
                    }
                }

                foreach (var output in _outputPlugins)
                {
                    if (eventList.Count > 0)
                    {
                        await output.TransferLogsAsync(eventList);
                    }
                }
            }
        }
    }
}
