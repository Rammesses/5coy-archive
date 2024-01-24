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
    }
}