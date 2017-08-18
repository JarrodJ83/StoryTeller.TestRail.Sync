using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fclp;
using Serilog;
using StoryTeller.Model.Persistence;
using StoryTeller.TestRail.Sync.TestRailClient;

namespace StoryTeller.TestRail.Sync
{
    class Program
    {
    
        private static ILogger Logger { get; set; }
       
        private static TestRailSyncClient TestRailClient;

        static void Main(string[] args)
        {
            try
            {
                Logger = new LoggerConfiguration()
                    .WriteTo.ColoredConsole()
                    .WriteTo.Seq("http://localhost:5341")
                    .MinimumLevel.Verbose()
                    .CreateLogger();
                var settings = new TestRailSyncSettingsFromArgs(args);

                var testRailClient = new TestRailSyncClient(settings.TestRailUrl);

                testRailClient.User = settings.Username;
                testRailClient.Password = settings.Password;

                var testRailSync = new TestRailSync(Logger, settings, testRailClient);

                testRailSync.Sync();

                Console.WriteLine("Any key to exit");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}