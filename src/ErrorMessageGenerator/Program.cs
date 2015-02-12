using System;
using System.Threading;
using EasyNetQ;

namespace ErrorMessageGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var bus = RabbitHutch.CreateBus("host=localhost"))
            {
                using (var processed = new AutoResetEvent(false))
                {
                    bus.Subscribe<Message>("test", m =>
                    {
                        processed.Set();
                        throw new Exception("error");
                    });

                    var msg = new Message { Field1 = "test" };
                    bus.Publish(msg);
                    processed.WaitOne();
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }
    }

    public class Message
    {
        public string Field1 { get; set; }
    }
}
