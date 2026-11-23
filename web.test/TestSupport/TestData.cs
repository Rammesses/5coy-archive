using System;

using Microsoft.Extensions.Logging.Abstractions;

using Models;

using Services;

namespace web.test.TestSupport;

/// <summary>Builds real services over small, hand-made data sets served by <see cref="StubHttpHandler"/>.</summary>
internal static class TestData
{
    public static MissionsService MissionsService(params Mission[] missions)
    {
        var http = new StubHttpHandler().ServeJson("data/missions.json", missions);
        return new MissionsService(http.CreateClient(), NullLogger<MissionsService>.Instance);
    }

    public static ArticlesService ArticlesService(Section<Article>[] sections, ArticleGroup[] groups, StubHttpHandler http = null)
    {
        http ??= new StubHttpHandler();
        http.ServeJson("data/sections.json", sections).ServeJson("data/articles.json", groups);
        return new ArticlesService(http.CreateClient(), NullLogger<ArticlesService>.Instance);
    }

    public static Mission Mission(string reference, params string[] related) => new()
    {
        Reference = reference,
        Name = $"Mission {reference}",
        StartDate = new DateTime(1994, 6, 10),
        EndDate = new DateTime(1994, 6, 12),
        Related = related,
    };

    public static Article Article(string reference, string missionRef = null, string[] related = null, params Article[] children) => new()
    {
        Reference = reference,
        Title = $"Title of {reference}",
        MissionRef = missionRef,
        Related = related ?? Array.Empty<string>(),
        Children = children,
    };

    public static Section<Article> Section(string reference, params Section<Article>[] children) => new()
    {
        Reference = reference,
        Title = $"Section {reference}",
        Children = children,
    };

    public static ArticleGroup Group(string reference, params Article[] articles) => new()
    {
        Reference = reference,
        Articles = articles,
    };
}
