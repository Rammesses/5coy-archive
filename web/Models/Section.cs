using System;

namespace Models
{   
    public class Section
    {
        public string Reference { get; set; }
        public string Title { get; set;}
        public string ContentUrl { get; set; }
        public string Glyph { get; set; }
        public string Url => $"/articles/{Reference}";
        public Article[] Articles { get; set; } = Array.Empty<Article>();
    }
}
