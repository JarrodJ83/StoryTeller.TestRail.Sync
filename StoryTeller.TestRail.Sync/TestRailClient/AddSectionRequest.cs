using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryTeller.TestRail.Sync.TestRailClient
{
    public class AddSectionRequest
    {
        public AddSectionRequest(int projectId, Section section)
        {
            ProjectId = projectId;
            Section = section;
        }

        public int ProjectId { get; private set; }
        public Section Section { get; private set; }
    }
}
