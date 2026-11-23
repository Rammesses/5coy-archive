using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Models;

namespace Services;

public interface IArticlesService : ISectionService<Article>, IItemsService<Article>
{
    /// <summary>The chain of sections (root first) that contains the given article.</summary>
    Task<Section<Article>[]> GetSectionPathForItem(Article article);

    /// <summary>Articles named in the article's Related list, plus articles whose Related list names it.</summary>
    Task<Article[]> GetRelatedItems(Article article);

    /// <summary>
    /// Articles linked to a mission without having it as their MissionRef: those named in the
    /// mission's Related list, plus articles whose Related list names the mission.
    /// </summary>
    Task<Article[]> GetRelatedItemsForMission(Mission mission);
}

public class ArticlesService : IArticlesService
{
    private ArticleGroup[] articleData = null;

    private Section<Article>[] sectionData = null;

    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public ArticlesService(HttpClient httpClient, ILogger<ArticlesService> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Section<Article>[]> GetSections()
    {
        await EnsureDataLoaded();

        return sectionData;
    }

    public async Task<Section<Article>> GetSection(string sectionRef)
    {
        await EnsureDataLoaded();

        if (sectionRef == Section<Article>.AllItemsRef)
        {
            return new Section<Article>() {
                Reference = Section<Article>.AllItemsRef,
                Title = $"All Articles"
            };
        }
        var section = Section<Article>.Flatten(sectionData).FirstOrDefault(s => s.Reference == sectionRef);
        return section;
    }

    public async Task<Article[]> GetItems(string sectionRef)
    {
        await EnsureDataLoaded();

        if (sectionRef == Section<Article>.AllItemsRef)
            return Article.Flatten(articleData.SelectMany(g => g.Articles)).ToArray();

        return await GetGroupArticles(sectionRef);
    }

    public async Task<Article[]> GetAllItemsInTree(string sectionRef)
    {
        await EnsureDataLoaded();

        if (sectionRef == Section<Article>.AllItemsRef)
            return Array.Empty<Article>();

        return Article.Flatten(await GetGroupArticles(sectionRef)).ToArray();
    }

    public async Task<Article> GetItem(string sectionRef, string articleRef)
    {
        await EnsureDataLoaded();

        var allArticles = sectionRef == Section<Article>.AllItemsRef ?
            articleData.SelectMany(g => g.Articles) :
            await GetAllItemsInTree(sectionRef);

        var article = allArticles.FirstOrDefault(a => a.Reference == articleRef);            
        return article;
    }

    public async Task<Article[]> GetItemsByMissionRef(string missionRef)
    {
        await EnsureDataLoaded();

        var articles = articleData.SelectMany(group => group.Articles).Where(a => a.MissionRef == missionRef).ToArray();

        return articles;
    }

    public async Task<Article[]> GetRelatedItems(Article article)
    {
        await EnsureDataLoaded();

        if (article == null)
            return Array.Empty<Article>();

        var allArticles = Article.Flatten(articleData.SelectMany(g => g.Articles)).ToArray();

        var linkedFrom = (article.Related ?? Array.Empty<string>())
            .Select(r => allArticles.FirstOrDefault(a => a.Reference == r))
            .Where(a => a != null);
        var linkedTo = allArticles.Where(a => a.Related?.Contains(article.Reference) == true);

        return linkedFrom.Concat(linkedTo)
            .Where(a => a.Reference != article.Reference)
            .GroupBy(a => a.Reference)
            .Select(g => g.First())
            .ToArray();
    }

    public async Task<Article[]> GetRelatedItemsForMission(Mission mission)
    {
        await EnsureDataLoaded();

        if (mission == null)
            return Array.Empty<Article>();

        var allArticles = Article.Flatten(articleData.SelectMany(g => g.Articles)).ToArray();

        var linkedFrom = (mission.Related ?? Array.Empty<string>())
            .Select(r => allArticles.FirstOrDefault(a => a.Reference == r))
            .Where(a => a != null);
        var linkedTo = allArticles.Where(a => a.Related?.Contains(mission.Reference) == true);

        return linkedFrom.Concat(linkedTo)
            .Where(a => a.MissionRef != mission.Reference)
            .GroupBy(a => a.Reference)
            .Select(g => g.First())
            .ToArray();
    }

    public async Task<Section<Article>> GetSectionForItem(string articleRef)
    {
        await EnsureDataLoaded();

        var group = articleData.FirstOrDefault(g => Article.Flatten(g.Articles).Any(a => a.Reference == articleRef));
        if (group == null)
            return null;

        return Section<Article>.Flatten(sectionData).FirstOrDefault(s => s.Reference == group.Reference);
    }

    public async Task<Section<Article>[]> GetSectionPathForItem(Article article)
    {
        await EnsureDataLoaded();

        if (article == null)
            return Array.Empty<Section<Article>>();

        // Prefer the group holding this exact instance - some articles are deliberately
        // duplicated across groups (e.g. In-Character and Mission Reports).
        var group = articleData.FirstOrDefault(g => Article.Flatten(g.Articles).Any(a => ReferenceEquals(a, article)))
                 ?? articleData.FirstOrDefault(g => Article.Flatten(g.Articles).Any(a => a.Reference == article.Reference));
        if (group == null)
            return Array.Empty<Section<Article>>();

        return FindSectionPath(sectionData, group.Reference) ?? Array.Empty<Section<Article>>();
    }

    // Top-level articles of the group for a section; empty (not a crash) when the section is
    // unknown (e.g. a mistyped /article/{section}/... URL) or has no article group.
    private async Task<Article[]> GetGroupArticles(string sectionRef)
    {
        var section = await GetSection(sectionRef);
        if (section == null)
            return Array.Empty<Article>();

        return articleData.FirstOrDefault(g => g.Reference == section.Reference)?.Articles
            ?? Array.Empty<Article>();
    }

    private static Section<Article>[] FindSectionPath(IEnumerable<Section<Article>> sections, string sectionRef)
    {
        foreach (var section in sections)
        {
            if (section.Reference == sectionRef)
                return new[] { section };

            var childPath = FindSectionPath(section.Children, sectionRef);
            if (childPath != null)
                return childPath.Prepend(section).ToArray();
        }

        return null;
    }
    
    private async Task EnsureDataLoaded()
    {
        // load the section data from `data/sections.json`
        if (sectionData == null)
        {
            var loadedData = await httpClient.GetFromJsonAsync<Section<Article>[]>("data/sections.json");
            lock(this)
            {
                sectionData = loadedData;
                // Top-level sections have no parent; their sub-sections point back at them
                // (used for the back-link on the section page).
                sectionData.PopulateParentage(null);
            }
        }

        if (articleData == null)
        {
            var loadedData = await httpClient.GetFromJsonAsync<ArticleGroup[]>("data/articles.json");
            foreach(var section in loadedData)
            {
                if (section.Data != null)
                {
                    var additionalArticles = await httpClient.GetFromJsonAsync<Article[]>(section.Data);
                    if (additionalArticles.Any())
                    {
                        var combinedArticles = section.Articles.ToList();
                        combinedArticles.AddRange(additionalArticles);
                        section.Articles = combinedArticles.ToArray();
                    }
                }
            }

            lock(this)
            {
                articleData = loadedData;
                foreach(var section in articleData)
                {
                    section.Articles.PopulateParentage(null);
                }
            }
        }
    }


}
