using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryTeller.TestRail.Sync.TestRailClient
{
    public interface ITestRailSyncClient
    {
        List<Case> GetCases(int projectId);
        void UpdateCase(Case testCase);
        void DeleteSection(int sectionId);
        void DeleteCase(int caseId);
        Section AddSection(AddSectionRequest request);
        Case AddCase(Case testCase);
        List<Section> GetSections(int projectId);
    }
}
