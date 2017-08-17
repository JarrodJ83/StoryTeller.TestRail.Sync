using Newtonsoft.Json;

namespace StoryTeller.TestRail.Sync.TestRailClient
{
    public class AddCaseRequest
    {
        public AddCaseRequest(string title, int sectionid = 0)
        {
            this.title = title;
            this.section_id = sectionid;
        }
        
        [JsonIgnore]
        public int section_id { get; set; }
        public string title { get; set; }
        public int? priority_id { get; set; }
    }
}
