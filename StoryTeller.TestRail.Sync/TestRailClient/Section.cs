using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryTeller.TestRail.Sync.TestRailClient
{
    public class Section
    {
        public int depth { get; set; }
        public int display_order { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public int? parent_id { get; set; }
        public int? suite_id { get; set; }
    }
}
