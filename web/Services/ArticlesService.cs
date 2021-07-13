using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Models;

namespace Services
{
    public interface IArticlesService
    {
        Task<Section[]> GetSections();
        Task<Section> GetSection(string sectionRef);
        Task<Article> GetArticle(string sectionRef, string articleRef);
    }

    public class ArticlesService : IArticlesService
    {
        private Section[] articleData = null;

        private readonly HttpClient httpClient;

        public ArticlesService(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException();
        }

        public async Task<Section[]> GetSections()
        {
            await EnsureDataLoaded();

            return articleData;
        }

        public async Task<Section> GetSection(string sectionRef)
        {
            await EnsureDataLoaded();

            return articleData.FirstOrDefault(s => s.Url.EndsWith(sectionRef));
        }

        public async Task<Article> GetArticle(string sectionRef, string articleRef)
        {
            await EnsureDataLoaded();

            var section = await GetSection(sectionRef);
            return section.Articles.FirstOrDefault(a => a.Reference == articleRef);
        }

        private async Task EnsureDataLoaded()
        {
            if (articleData == null)
            {
                var loadedData = await httpClient.GetFromJsonAsync<Section[]>("data/articles.json");
                lock(this)
                {
                    articleData = loadedData;
                }
            }
        }
    }
}
    