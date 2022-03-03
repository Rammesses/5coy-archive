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
        Task<Article[]> GetArticles(string sectionRef);
        Task<Article[]> GetAllArticlesInTree(string sectionRef);
        Task<Article> GetArticle(string sectionRef, string articleRef);
    }

    public class ArticlesService : IArticlesService
    {
        private ArticleGroup[] articleData = null;

        private Section[] sectionData = null;

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

            return sectionData;
        }

        public async Task<Section> GetSection(string sectionRef)
        {
            await EnsureDataLoaded();

            var section = Section.Flatten(sectionData).FirstOrDefault(s => s.Reference == sectionRef);
            return section;
        }

        public async Task<Article[]> GetArticles(string sectionRef)
        {
            await EnsureDataLoaded();

            var section = await GetSection(sectionRef);
            var sectionArticles = articleData.FirstOrDefault(s => s.Reference == section.Reference).Articles;
            return sectionArticles;
        }

        public async Task<Article[]> GetAllArticlesInTree(string sectionRef)
        {
            await EnsureDataLoaded();

            var section = await GetSection(sectionRef);
            var sectionArticles = articleData.FirstOrDefault(s => s.Reference == section.Reference).Articles;
            var allArticles = Article.Flatten(sectionArticles);
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
            // load the section data from `data/sections.json`
            if (sectionData == null)
            {
                var loadedData = await httpClient.GetFromJsonAsync<Section[]>("data/sections.json");
                lock(this)
                {
                    sectionData = loadedData;
                    foreach (var section in sectionData)
                    {
                        PopulateParentage(section.Children, null);
                    }
                }
            }

            if (articleData == null)
            {
                var loadedData = await httpClient.GetFromJsonAsync<ArticleGroup[]>("data/articles.json");
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

        private void PopulateParentage<T>(IHeirarchicalItem<T>[] items, T parent)
            where T : IHeirarchicalItem<T>
        {
            foreach (var item in items.OfType<T>())
            {
                if (item.Parent == null)
                {
                    item.Parent = parent;
                }

                var children = item.Children.OfType<IHeirarchicalItem<T>>().ToArray();
                if (children.Any())
                {
                    PopulateParentage<T>(children, item);
                }
            }
        }
    }
}
    