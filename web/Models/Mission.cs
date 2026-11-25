using System;
using System.Collections.Generic;
using System.Globalization;

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

        /// <summary>
        /// The standard text for any link to this mission:
        /// "Mission '{name}' - {month} {year} ({planet})", in in-game years. The planet is left
        /// out when it isn't known. Keep in step with the steering in CLAUDE.md.
        /// </summary>
        public string LinkText
        {
            get
            {
                var date = EndDate.AddYears(500).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                var planet = string.IsNullOrWhiteSpace(Planet) || Planet == "None" || Planet == "?"
                    ? string.Empty : $" ({Planet})";
                return $"{Name} - {date}{planet}";
            }
        }
    }
}