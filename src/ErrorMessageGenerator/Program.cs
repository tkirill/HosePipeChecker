using System;
using System.Collections.Generic;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using EasyNetQ;

namespace ErrorMessageGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                using (var bus = RabbitHutch.CreateBus(GetConnectionString(options)))
                {
                    using (var processed = new CountdownEvent(options.Count))
                    {
                        bus.Subscribe<Message>("test", m =>
                        {
                            if (!processed.IsSet)
                            {
                                processed.Signal();
                                throw new Exception("error");
                            }
                        });
                        
                        for (var i = 0; i < options.Count; i++)
                        {
                            var msg = new Message {Field1 = "test", Field2 = 10};
                            bus.Publish(msg);
                        }

                        processed.Wait(TimeSpan.FromMinutes(2));
                        Console.WriteLine("Processed");
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                        Console.WriteLine("Done");
                    }
                }
            }
        }

        private static string GetConnectionString(Options options)
        {
            var parts = new List<string>();

            parts.Add(string.Format("host={0}", options.Host));
            if (!string.IsNullOrEmpty(options.User))
                parts.Add(string.Format("username={0}", options.User));
            if (!string.IsNullOrEmpty(options.Password))
                parts.Add(string.Format("password={0}", options.Password));

            return string.Join(";", parts);
        }
    }

    public class Options
    {
        [Option('h', "host", DefaultValue = "localhost", HelpText = "RabbitMQ host")]
        public string Host { get; set; }

        [Option('u', "user", HelpText = "RabbitMQ user")]
        public string User { get; set; }

        [Option('p', "password", HelpText = "RabbitMQ password")]
        public string Password { get; set; }

        [Option('c', "count", DefaultValue = 10, HelpText = "Number of errors")]
        public int Count { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, x => HelpText.DefaultParsingErrorsHandler(this, x));
        }
    }

    public class Message
    {
        public string Field1 { get; set; }
        public int Field2 { get; set; }
    }
}