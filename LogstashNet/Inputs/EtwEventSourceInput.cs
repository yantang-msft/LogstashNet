using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogstashNet.Inputs
{
    internal class EtwEventSourceInput : InputBase
    {
        private class EtwEventSourceListener : EventListener
        {
            private List<string> _providers;
            private Action<EventWrittenEventArgs> _onEventWrittenAction;
            private List<EventSource> _eventSourcesToEnable = new List<EventSource>();

            public EtwEventSourceListener(List<string> providers, Action<EventWrittenEventArgs> onEventWrittenAction)
            {
                _providers = providers;
                _onEventWrittenAction = onEventWrittenAction;

                // TODO: check why OnEventSourceCreated is triggerred before the EventListener constructor, and remove the logic below if possible
                foreach (var eventSource in _eventSourcesToEnable)
                {
                    if (IsIntendedEventSource(eventSource))
                    {
                        this.EnableEvents(eventSource, EventLevel.LogAlways);
                    }
                }
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (_providers == null)
                {
                    _eventSourcesToEnable.Add(eventSource);
                }

                if (IsIntendedEventSource(eventSource))
                {
                    this.EnableEvents(eventSource, EventLevel.LogAlways);
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (_onEventWrittenAction != null)
                {
                    _onEventWrittenAction(eventData);
                }
            }

            private bool IsIntendedEventSource(EventSource eventSource)
            {
                if (_providers == null || _providers.Count == 0)
                {
                    return false;
                }

                if (_providers.Contains(eventSource.Name))
                {
                    return true;
                }
                else
                {
                    foreach (var provider in _providers)
                    {
                        Guid enabledProviderGuid;
                        if (Guid.TryParse(provider, out enabledProviderGuid) && enabledProviderGuid == eventSource.Guid)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private EtwEventSourceListener _listener;
        private ConcurrentQueue<JObject> _messageQueue;

        public EtwEventSourceInput(List<string> providers, ConcurrentQueue<JObject> messageQueue)
        {
            _listener = new EtwEventSourceListener(providers, OnEventWritten);
            _messageQueue = messageQueue;
        }

        private void OnEventWritten(EventWrittenEventArgs eventData)
        {
            try
            {
                var serializedEvent = JsonConvert.SerializeObject(eventData);
                _messageQueue.Enqueue(JsonConvert.DeserializeObject<JObject>(serializedEvent));
            }
            catch
            {
                // TODO: exception handling
            }
        }
    }
}
