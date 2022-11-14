using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Ganss.Xss;

using Markdig;

namespace Services
{
    public interface IMarkdownService
    {
        Task<string> GetContentAsync(string contentUrl);
    }

    public class MarkdownService : IMarkdownService
    {
        private readonly HttpClient httpClient;
        private readonly IHtmlSanitizer htmlSanitizer;

        public MarkdownService(
            HttpClient httpClient, 
            IHtmlSanitizer htmlSanitizer)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.htmlSanitizer = htmlSanitizer ?? throw new ArgumentNullException(nameof(htmlSanitizer));
        }
        
        public async Task<string> GetContentAsync(string contentUrl)
        {
            if (string.IsNullOrEmpty(contentUrl))
                return null;

            var markdown = await httpClient.GetStringAsync(contentUrl);
            
            var html = Markdig.Markdown.ToHtml(
                markdown, 
                new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());

            var sanitizedHtml = this.htmlSanitizer.Sanitize(html);
            return sanitizedHtml;
        }
    }
}