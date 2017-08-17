using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryTeller.TestRail.Sync.TestRailClient
{
    public class Case
    {
        public int id { get; set; }
        public string title { get; set; }
        public int section_id { get; set; }
        public int? priority_id { get; set; }
    }
}
