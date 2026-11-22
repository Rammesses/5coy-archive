using System;
using System.Collections.Generic;

namespace Models
{
    public class Mission
    {
        public string Reference { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Name { get; set; }

        public string Summary { get; set; }

        public string Location { get; set; }
        public string Planet { get; set; }

        public string ContentUrl { get; set; }

        /// <summary>
        /// References of articles and missions linked from this mission's summary (other than
        /// articles that already have this mission as their MissionRef).
        /// </summary>
        public string[] Related { get; set; } = Array.Empty<string>();
    }
}