using System;
using System.CodeDom;
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

        private static APIClient TestRailClient;

        static void Main(string[] args)
        {
            try
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
                    if (!File.Exists(CredentialsFile))
                        throw new Exception("Could not load credentials file");

                    List<string> credentialsFile = File.ReadLines(CredentialsFile).ToList();

                    if (credentialsFile.Count < 2)
                        throw new Exception(
                            "File is expected to have at least two lines. The first contianing the username and the second containing the password");

                    Username = credentialsFile[0];
                    Password = credentialsFile[1];
                }

                TestRailClient = new APIClient(TestRailUrl);

                TestRailClient.User = Username;
                TestRailClient.Password = Password;


                //Purge();

                Run();

                Console.WriteLine("Any key to exit");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        static void Run()
        {
            List<Case> testCases = TestRailClient.GetCases(ProjectId);

            Logger.Debug("Found {NumberOfCases} cases for Project {ProjectId}", testCases.Count, ProjectId);

            Hierarchy hierarchy = HierarchyLoader
                .ReadHierarchy(SpecsFolder)
                .ToHierarchy();
            
            List<Section> sections = TestRailClient.GetSections(ProjectId);
            
            foreach (var spec in hierarchy.Specifications)
            {
                Logger.Verbose("Processing spec {SpecName}", spec.name);

                List<Section> suiteSections = CreateSectionsFromSuitePath(spec.SuitePath()).ToList();

                ProcessSuitePath(sections, suiteSections);

                int sectionId = suiteSections.Last().id;

                List<int> caseIds = TestCaseParser.ParseTestCaseIds(spec.name).ToList();

                Case existingCase = null;

                if (caseIds.Any())
                {
                    existingCase = testCases.FirstOrDefault(c => c.id == caseIds.First());
                }

                if (existingCase != null)
                {
                    Logger.Verbose("Spec {SpecName} is tied to C{CNumber}. Updating Case in TestRail", spec.name,
                        caseIds.First());

                    existingCase.title = spec.name;
                    existingCase.section_id = sectionId;

                    Logger.Verbose("Updating {SpecName} in TestRail", spec.name);
                }
                else
                {
                    Logger.Verbose("Adding {SpecName} to TestRail", spec.name);
                    
                    Case newCase = TestRailClient.AddCase(new Case
                    {
                        title = spec.name,
                        section_id = sectionId
                    });
                    
                    Logger.Verbose("Case {@Case} added to TestRail", newCase);

                    var specFile = File.ReadAllText(spec.Filename);

                    specFile = specFile.Replace(spec.name, $"{spec.name} [C{newCase.id}]");

                    File.WriteAllText(spec.Filename, specFile);
                }
            }
        }

        static IEnumerable<Section> CreateSectionsFromSuitePath(string suitePath)
        {
            string[] suiteSections = suitePath.Split('/');
            for (int i = 0; i < suiteSections.Length; i++)
            {
                yield return new Section
                {
                    name = suiteSections[i],
                    depth = i
                };
            }
        }

        static void ProcessSuitePath(List<Section> currentSections, List<Section> suiteSections)
        {
            for (var i = 0; i < suiteSections.Count; i++)
            {
                Section suiteSection = suiteSections[i];
                Section existingSection =
                    currentSections.FirstOrDefault(s =>
                        s.depth == suiteSection.depth &&
                        s.name.Equals(suiteSection.name, StringComparison.InvariantCultureIgnoreCase));

                if (existingSection == null)
                {
                    int? previousIndex = i == 0 ? new int?() : (i - 1);
                    Section parentSection = currentSections.FirstOrDefault(s => s.depth == suiteSection.depth - 1 &&
                                                                                    (previousIndex == null || 
                                                                                    suiteSections[previousIndex.Value].id == s.id));

                    if (parentSection != null)
                    {
                        suiteSection.parent_id = parentSection.id;
                    }

                    Section addedSection = TestRailClient.AddSection(new AddSectionRequest(ProjectId, suiteSection));

                    currentSections.Add(addedSection);

                    suiteSection.id = addedSection.id;
                }
                else
                {
                    suiteSection.id = existingSection.id;
                }
            }
        }

        static void Purge()
        {
            List<Case> testCases = TestRailClient.GetCases(ProjectId);

            foreach (Case testCase in testCases)
            {
                Logger.Warning("Deleting {CaseId}", testCase.id);
                TestRailClient.DeleteCase(testCase.id);
            }

            List<Section> sections = TestRailClient.GetSections(ProjectId);

            foreach (Section section in sections)
            {
                Logger.Warning("Deleting {SectionId}", section.id);
                TestRailClient.DeleteSection(section.id);
            }
        }
    }
}