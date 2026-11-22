using System;
using System.Collections.Generic;

namespace Models
{
    public class ArticleGroup
    {
        public string Reference { get; set; }
        public Article[] Articles { get; set; } = Array.Empty<Article>();
        public string Data { get; set; }

    }
    
    public class Article : IHeirarchicalItem<Article>
    {
        public string Reference { get; set; }
        public string Title { get; set; }
        public string ContentUrl { get; set; }
        public string PdfUrl { get; set; }

        public string MissionRef { get; set; }

        /// <summary>
        /// References of related articles or missions that this article doesn't report on
        /// (background, build-up, follow-ups). Shown under "Related Articles" on the article page.
        /// </summary>
        public string[] Related { get; set; } = Array.Empty<string>();
        public Article Parent { get; set; }

        public Article[] Children { get; set; } = Array.Empty<Article>();

        public static IEnumerable<Article> Flatten (IEnumerable<Article> list)
            => Extensions.EnumerableExtensions.Flatten<Article>(list, a => a.Children);
    }
}