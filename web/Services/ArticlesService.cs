using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Models;

namespace Services
{
    public interface IArticlesService
    {
        Task<Section[]> GetSections();
        Task<Section> GetSection(string sectionRef);
        Task<Article[]> GetAllArticlesInTree(string sectionRef);
        Task<Article> GetArticle(string sectionRef, string articleRef);
    }

    public class ArticlesService : IArticlesService
    {
        private Section[] articleData = null;

        private readonly HttpClient httpClient;
        private readonly ILogger logger;

        public ArticlesService(HttpClient httpClient, ILogger<ArticlesService> logger)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Section[]> GetSections()
        {
            await EnsureDataLoaded();

            return articleData;
        }

        public async Task<Section> GetSection(string sectionRef)
        {
            await EnsureDataLoaded();

            var section = articleData.FirstOrDefault(s => s.Url.EndsWith(sectionRef));
            return section;
        }

        public async Task<Article[]> GetAllArticlesInTree(string sectionRef)
        {
            await EnsureDataLoaded();

            var section = await GetSection(sectionRef);
            var allArticles = Article.Flatten(section.Articles);
            return allArticles.ToArray();
        }

        public async Task<Article> GetArticle(string sectionRef, string articleRef)
        {
            await EnsureDataLoaded();

            var allArticles = await GetAllArticlesInTree(sectionRef);
            var article = allArticles.FirstOrDefault(a => a.Reference == articleRef);            
            return article;
        }

        private async Task EnsureDataLoaded()
        {
            if (articleData == null)
            {
                var loadedData = await httpClient.GetFromJsonAsync<Section[]>("data/articles.json");
                lock(this)
                {
                    articleData = loadedData;
                    foreach(var section in articleData)
                    {
                        PopulateParentage(section.Articles, null);
                    }
                }
            }
        }

        private void PopulateParentage(Article[] articles, Article parent)
        {
            foreach (var article in articles)
            {
                if (article.Parent != parent)
                {
                    article.Parent = parent;
                }

                if (article.Children.Any())
                {
                    PopulateParentage(article.Children, article);
                }
            }
        }
    }
}
    