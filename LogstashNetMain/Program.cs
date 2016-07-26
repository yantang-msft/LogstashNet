using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogstashNetMain
{
    [EventSource(Name = "MyEventSource")]
    public sealed class MyEventSource : EventSource
    {
        public static MyEventSource log = new MyEventSource();

        [Event(1)]
        public void TestWriteEvent(string message, int num1)
        {
            base.WriteEvent(1, message, num1);
        }
    }

    [EventSource(Name = "MetricSource")]
    public sealed class MetricSource : EventSource
    {
        public static MetricSource log = new MetricSource();

        [Event(1)]
        public void WriteMetric(double value)
        {
            base.WriteEvent(1, value);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var manager = new LogstashNet.LogstashNetManager(@".\ConfigFiles\ETWAsMetrics.json");

            //int count = 0;
            //while (++count > 0)
            //{
            //    MyEventSource.log.TestWriteEvent(DateTime.Now.ToString(), count);
            //    System.Threading.Thread.Sleep(100);
            //}

            MyEventSource.log.TestWriteEvent("This is the message at " + DateTime.Now.ToString(), new Random().Next());
            MetricSource.log.WriteMetric(new Random().NextDouble());

            Console.ReadLine();
        }
    }
}
