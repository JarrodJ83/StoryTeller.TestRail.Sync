using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;
using StoryTeller.Model.Persistence;
using StoryTeller.TestRail.Sync.TestRailClient;

namespace StoryTeller.TestRail.Sync
{
    public class TestRailSync
    {
        private readonly ILogger _logger;
        private readonly ITestRailSyncSettings _settings;
        private readonly APIClient _testRailClient;

        public TestRailSync(ILogger logger, ITestRailSyncSettings settings, APIClient testRailClient)
        {
            _logger = logger;
            _settings = settings;
            _testRailClient = testRailClient;
        }

        public void Sync()
        {
            if (_settings.PurgeTestRail)
                Purge();

            List<Case> testCases = _testRailClient.GetCases(_settings.ProjectId);

            _logger.Debug("Found {NumberOfCases} cases for Project {ProjectId}", testCases.Count, _settings.ProjectId);

            Hierarchy hierarchy = HierarchyLoader
                .ReadHierarchy(_settings.SpecsFolder)
                .ToHierarchy();

            List<Section> sections = _testRailClient.GetSections(_settings.ProjectId);

            foreach (var spec in hierarchy.Specifications)
            {
                _logger.Verbose("Processing spec {SpecName}", spec.name);

                List<Section> suiteSections = CreateSectionsFromSuitePath(spec.SuitePath()).ToList();

                ProcessSuitePath(sections, suiteSections);

                Section section = suiteSections.Last();

                List<int> caseIds = TestCaseParser.ParseTestCaseIds(spec.name).ToList();

                Case existingCase = null;

                if (caseIds.Any())
                {
                    existingCase = testCases.FirstOrDefault(c => c.id == caseIds.First());
                }

                if (existingCase != null)
                {
                    _logger.Verbose("Spec {SpecName} is tied to C{CNumber}", spec.name, caseIds.First());

                    var cleanSpecName = CleanSpecName(spec.name);

                    if (existingCase.title != cleanSpecName)
                    {
                        existingCase.title = cleanSpecName;

                        _logger.Verbose("Updating {SpecName} in TestRail", spec.name);

                        _testRailClient.UpdateCase(existingCase);
                    }

                    if (existingCase.section_id != section.id)
                    {
                        existingCase.section_id = section.id;

                        _logger.Warning("{SpecName} will need manually moved to Section {@Section} in TestRail. Their API doesn't support moves", spec.name, section);
                    }
                }
                else
                {
                    _logger.Verbose("Adding {SpecName} to TestRail", spec.name);

                    Case newCase = _testRailClient.AddCase(new Case
                    {
                        title = spec.name,
                        section_id = section.id
                    });

                    _logger.Verbose("Case {@Case} added to TestRail", newCase);

                    var specFile = File.ReadAllText(spec.Filename);

                    specFile = specFile.Replace(spec.name, $"{spec.name} [C{newCase.id}]");

                    File.WriteAllText(spec.Filename, specFile);
                }
            }
        }

        string CleanSpecName(string specName)
        {
            var matches = TestCaseParser.TestCaseRegex.Matches(specName);

            foreach (Match match in matches)
            {
                specName = specName.Replace(match.Value, string.Empty);
            }

            return specName.Trim();
        }

        public void Purge()
        {
            Console.WriteLine($"Are you sure you wish to continue? This will delete all cases and sections in Project {_settings.ProjectId}");
            Console.WriteLine("[y] to continue or any other key to skip");
            var input = Console.ReadLine();

            if (!input.Equals("y", StringComparison.InvariantCultureIgnoreCase))
                return;

            List<Section> sections = _testRailClient.GetSections(_settings.ProjectId);
            foreach (Section section in sections.Where(s => s.depth == 0))
            {
                _logger.Warning("Deleting section {SectionId}", section.id);
                _testRailClient.DeleteSection(section.id);
            }

            List<Case> testCases = _testRailClient.GetCases(_settings.ProjectId);
            foreach (Case testCase in testCases)
            {
                _logger.Warning("Deleting case {CaseId}", testCase.id);
                _testRailClient.DeleteCase(testCase.id);
            }
        }

        IEnumerable<Section> CreateSectionsFromSuitePath(string suitePath)
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

        void ProcessSuitePath(List<Section> currentSections, List<Section> suiteSections)
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

                    Section addedSection = _testRailClient.AddSection(new AddSectionRequest(_settings.ProjectId, suiteSection));

                    currentSections.Add(addedSection);

                    suiteSection.id = addedSection.id;
                }
                else
                {
                    suiteSection.id = existingSection.id;
                }
            }
        }
    }
}
