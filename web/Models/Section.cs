using System;
using System.Collections.Generic;

namespace Models
{   
    public class Section : IHeirarchicalItem<Section>
    {
        public string Reference { get; set; }
        public string Title { get; set;}
        public string ContentUrl { get; set; }
        public string Glyph { get; set; }

        public string Url => $"/articles/{Reference}";

        public Article[] Articles { get; set; } = Array.Empty<Article>();

        public Section[] Children { get; set; } = Array.Empty<Section>();

        public Section Parent { get; set; }

        public static IEnumerable<Section> Flatten (IEnumerable<Section> list)
            => Extensions.EnumerableExtensions.Flatten<Section>(list, a => a.Children);

    }
}
