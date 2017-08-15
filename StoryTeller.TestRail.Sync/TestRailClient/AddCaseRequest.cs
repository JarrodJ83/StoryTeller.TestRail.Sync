using Newtonsoft.Json;

namespace StoryTeller.TestRail.Sync.TestRailClient
{
    public class AddCaseRequest
    {
        public AddCaseRequest(int sectionId, string title)
        {
            section_id = sectionId;
            this.title = title;
        }

        [JsonIgnore]
        public int section_id { get; set; }
        public string title { get; set; }
        public int? priority_id { get; set; }
    }
}
