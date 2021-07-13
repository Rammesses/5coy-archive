using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Services
{
    public interface IMarkdownService
    {
        Task<string> GetContent(string contentUrl);
    }

    public class MarkdownService : IMarkdownService
    {
        private readonly HttpClient httpClient;

        public MarkdownService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }
        public async Task<string> GetContent(string contentUrl)
        {
            if (string.IsNullOrEmpty(contentUrl))
                return null;

            // var markdown = httpClient.GetStringAsync(contentUrl);

            await Task.Delay(1000);

            return $"Content from '{contentUrl}' goes here.";
        }
    }
}