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
