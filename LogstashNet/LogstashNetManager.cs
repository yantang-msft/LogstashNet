using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private List<OutputBase> _outputPlugins = new List<OutputBase>();
        private int _batchSize;
        private int _batchDelay;

        public LogstashNetManager(string configFilePath, int workerThreadNum = 8, int batchSize = 50, int batchDelay = 1000)
        {
            _batchSize = batchSize;
            _batchDelay = batchDelay;

            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));

            // Initialize input plugins
            if (config.Input != null)
            {
                if (config.Input.EtwEventSource != null)
                {
                    _inputPlugins.Add(new EtwEventSourceInput(config.Input.EtwEventSource.providers, _messageQueue));
                }
            }

            // Initialize output plugins
            if (config.Output != null)
            {
                if (config.Output.StdOut != null)
                {
                    _outputPlugins.Add(new StdOutput());
                }
            }

            // TODO: Plugin extensibility

            // Start log forwarding
            for (int i = 0; i < workerThreadNum; i++)
            {
                Task.Run(() => ForwardLogs());
            }

            // TODO: handle corrupt and restart scenario
        }

        private async void ForwardLogs()
        {
            // TODO: Is a shutdown function needed? If so, pass in an CancellationToken to terminate
            while (true)
            {
                var messageList = new List<JObject>();
                var endTime = DateTime.Now.AddMilliseconds(_batchDelay);

                JObject message = null;
                while (messageList.Count < _batchSize && _messageQueue.TryDequeue(out message))
                {
                    messageList.Add(message);
                }

                // Didn't fetch enough items, which means the input load is not high.
                if (messageList.Count < _batchSize)
                {
                    var currentTime = DateTime.Now;
                    if (endTime > currentTime)
                    {
                        await Task.Delay((endTime - currentTime).Milliseconds);
                    }
                }

                foreach (var output in _outputPlugins)
                {
                    await output.TransferLogsAsync(messageList);
                }
            }
        }
    }
}
