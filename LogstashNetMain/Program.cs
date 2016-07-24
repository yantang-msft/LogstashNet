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
        public void TestWriteEvent(string timeStamp, int num1)
        {
            base.WriteEvent(1, timeStamp, num1);
        }
    }

        class Program
    {
        static void Main(string[] args)
        {
            var manager = new LogstashNet.LogstashNetManager(@".\ConfigFiles\simple.json");

            int count = 0;
            while (++count > 0)
            {
                //MyEventSource.log.TestWriteEvent(DateTime.Now.ToString(), count);
                System.Threading.Thread.Sleep(100);
            }

            Console.ReadLine();
        }
    }
}
