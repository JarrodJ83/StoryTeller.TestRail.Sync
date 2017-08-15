using Newtonsoft.Json;

namespace StoryTeller.TestRail.Sync.TestRailClient
{
    public class AddCaseRequest
    {
        public AddCaseRequest(string title)
        {
            this.title = title;
        }
        
        public string title { get; set; }
        public int? priority_id { get; set; }
    }
}
