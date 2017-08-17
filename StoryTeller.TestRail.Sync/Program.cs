using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Fclp;
using Fclp.Internals;
using Newtonsoft.Json;
using Serilog;
using StoryTeller.Model.Persistence;
using StoryTeller.TestRail.Sync.TestRailClient;

namespace StoryTeller.TestRail.Sync
{
    class Program
    {
        private static int ProjectId { get; set; }
        private static string TestRailUrl { get; set; }
        private static string Username { get; set; }
        private static string Password { get; set; }
        private static string SpecsFolder { get; set; }
        private static string CredentialsFile { get; set; }
        private static ILogger Logger { get; set; }
        private const int TestCasesSectionId = 1;
        
        static void Main(string[] args)
        {
            Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .WriteTo.Seq("http://localhost:5341")
                .MinimumLevel.Verbose()
                .CreateLogger();

            var parser = new FluentCommandLineParser();
            
            parser.Setup<int>("projectid")
                .Callback(value => ProjectId = value)
                .Required();

            parser.Setup<string>("password")
                .Callback(value => Password = value);

            parser.Setup<string>("username")
                .Callback(value => Username = value);

            parser.Setup<string>("credentialsfile")
                .Callback(value => CredentialsFile = value);

            parser.Setup<string>("testrailurl")
                .Callback(value => TestRailUrl = value)
                .Required();

            parser.Setup<string>("specsfolder")
                .Callback(value => SpecsFolder = value)
                .Required();

            parser.Parse(args);

            if (!string.IsNullOrEmpty(CredentialsFile))
            {
                if(!File.Exists(CredentialsFile))
                    throw new Exception("Could not load credentials file");

                List<string> credentialsFile = File.ReadLines(CredentialsFile).ToList();

                if(credentialsFile.Count < 2)
                    throw new Exception("File is expected to have at least two lines. The first contianing the username and the second containing the password");

                Username = credentialsFile[0];
                Password = credentialsFile[1];
            }

            Run();

            Console.WriteLine("Any key to exit");
            Console.ReadKey();
        }

        static void Run()
        {
            try
            {
                var testRailClient = new APIClient(TestRailUrl);

                testRailClient.User = Username;
                testRailClient.Password = Password;

                object result = testRailClient.SendGet($"get_cases/{ProjectId}");

                List<Case> testCases = JsonConvert.DeserializeObject<GetCasesResponse>(result.ToString());

                Logger.Debug("Found {NumberOfCases} cases for Project {ProjectId}", testCases.Count, ProjectId);

                Hierarchy hierarchy = HierarchyLoader
                    .ReadHierarchy(SpecsFolder)
                    .ToHierarchy();

                foreach (var spec in hierarchy.Specifications)
                {
                    Logger.Verbose("Processing spec {SpecName}", spec.name);

                    List<int> caseIds = TestCaseParser.ParseTestCaseIds(spec.name).ToList();

                    if (caseIds.Any())
                    {
                        Logger.Verbose("Spec {SpecName} is tied to C{CNumber}. Updating Case in TestRail", spec.name,
                            caseIds.First());
                    }
                    else
                    {
                        Logger.Verbose("Adding {SpecName} to TestRail", spec.name);

                        var addCaseRequest = new AddCaseRequest(spec.name);

                        object response = testRailClient.SendPost($"add_case/{TestCasesSectionId}", addCaseRequest);

                        var newCase = JsonConvert.DeserializeObject<Case>(response.ToString());

                        Logger.Verbose("Case {@Case} added to TestRail", newCase);

                        var specFile = File.ReadAllText(spec.Filename);

                        specFile = specFile.Replace(spec.name, $"{spec.name} [C{newCase.id}]");

                        File.WriteAllText(spec.Filename, specFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
