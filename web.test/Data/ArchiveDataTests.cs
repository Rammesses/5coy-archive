using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Models;

using NUnit.Framework;

using Services;

namespace web.test.Data;

/// <summary>
/// Runs the real web/wwwroot data through the real services and C# models. The Python
/// crossref checker covers reference integrity; these catch drift between the JSON and the
/// models (misspelt or renamed properties, wrong types) that only shows up in the app.
/// </summary>
public class ArchiveDataTests
{
    private static readonly string WwwRoot = FindWwwRoot();

    private ArticlesService articles;
    private MissionsService missions;
    private Mission[] allMissions;
    private Article[] allArticles;

    [OneTimeSetUp]
    public async Task LoadArchive()
    {
        var http = new HttpClient(new WwwRootHandler(WwwRoot)) { BaseAddress = new Uri("http://localhost/") };
        articles = new ArticlesService(http, NullLogger<ArticlesService>.Instance);
        missions = new MissionsService(http, NullLogger<MissionsService>.Instance);

        allMissions = await missions.GetAllMissions();
        allArticles = await articles.GetItems(Section<Article>.AllItemsRef);
    }

    // ---- JSON matches the models -----------------------------------------------------------

    // Same settings the app's GetFromJsonAsync uses (case-insensitive), but rejecting unknown properties.
    private static readonly JsonSerializerOptions Strict = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static IEnumerable<string> ArticleDataFiles()
        => Directory.EnumerateFiles(Path.Combine(FindWwwRoot(), "data", "articles"), "*.json")
                    .Select(Path.GetFileName)
                    .Order();

    [Test]
    public void Missions_json_has_no_unknown_properties()
        => Read<Mission[]>("data/missions.json").Should().NotBeEmpty();

    [Test]
    public void Articles_json_has_no_unknown_properties()
        => Read<ArticleGroup[]>("data/articles.json").Should().NotBeEmpty();

    [TestCaseSource(nameof(ArticleDataFiles))]
    public void Article_data_file_has_no_unknown_properties(string file)
        => Read<Article[]>($"data/articles/{file}").Should().NotBeNull();

    [Test]
    public void Videos_json_has_no_unknown_properties()
        => Read<VideoGroup[]>("data/videos.json").Should().NotBeEmpty();

    private static IEnumerable<string> VideoDataFiles()
        => Directory.EnumerateFiles(Path.Combine(FindWwwRoot(), "data", "videos"), "*.json")
                    .Select(Path.GetFileName)
                    .Order();

    [TestCaseSource(nameof(VideoDataFiles))]
    public void Video_data_file_has_no_unknown_properties(string file)
        => Read<Video[]>($"data/videos/{file}").Should().NotBeNull();

    [Test]
    public async Task Every_video_has_a_unique_reference_a_YouTube_id_and_resolving_links()
    {
        var videos = await new VideoService(
            new HttpClient(new WwwRootHandler(WwwRoot)) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<VideoService>.Instance).GetAllVideos();
        var known = allMissions.Select(m => m.Reference).Concat(allArticles.Select(a => a.Reference)).ToHashSet();
        var missionRefs = allMissions.Select(m => m.Reference).ToHashSet();

        var problems = videos.GroupBy(v => v.Reference).Where(g => g.Count() > 1).Select(g => $"duplicate {g.Key}")
            .Concat(videos.Where(v => string.IsNullOrEmpty(v.YouTubeVideoId)).Select(v => $"{v.Reference}: no YouTubeVideoId"))
            .Concat(videos.Where(v => !string.IsNullOrEmpty(v.MissionRef) && !missionRefs.Contains(v.MissionRef)).Select(v => $"{v.Reference}: MissionRef {v.MissionRef}"))
            .Concat(videos.SelectMany(v => (v.Related ?? Array.Empty<string>()).Where(r => !known.Contains(r)).Select(r => $"{v.Reference}: Related {r}")))
            .Concat(videos.Where(v => !string.IsNullOrEmpty(v.ContentUrl) && !File.Exists(LocalPath(v.ContentUrl))).Select(v => $"{v.Reference}: ContentUrl {v.ContentUrl}"));

        string.Join(Environment.NewLine, problems).Should().BeEmpty();
    }

    [Test]
    public void Sections_json_has_no_unknown_properties()
        => Read<Section<Article>[]>("data/sections.json").Should().NotBeEmpty();

    [Test]
    public void Every_article_data_file_is_used_by_a_group()
    {
        var used = Read<ArticleGroup[]>("data/articles.json").Where(g => g.Data != null).Select(g => Path.GetFileName(g.Data));

        ArticleDataFiles().Should().BeSubsetOf(used);
    }

    // ---- the services can serve every page -------------------------------------------------

    [Test]
    public async Task Every_section_loads_through_the_service()
    {
        var sections = Section<Article>.Flatten(await articles.GetSections()).ToArray();

        foreach (var section in sections)
        {
            (await articles.GetItems(section.Reference)).Should().NotBeNull(section.Reference);
            (await articles.GetAllItemsInTree(section.Reference)).Should().NotBeNull(section.Reference);
        }
    }

    [Test]
    public async Task Every_article_group_belongs_to_a_section()
    {
        var sectionRefs = Section<Article>.Flatten(await articles.GetSections()).Select(s => s.Reference).ToHashSet();

        Read<ArticleGroup[]>("data/articles.json").Select(g => g.Reference)
            .Where(r => !sectionRefs.Contains(r)).Should().BeEmpty();
    }

    [Test]
    public async Task Every_article_resolves_to_a_section_path()
    {
        var missing = new List<string>();
        foreach (var article in allArticles.Where(a => !string.IsNullOrEmpty(a.Reference)))
        {
            if ((await articles.GetSectionPathForItem(article)).Length == 0)
                missing.Add(article.Reference);
        }

        missing.Should().BeEmpty();
    }

    [Test]
    public void Mission_references_are_unique()
        // Placeholder rows for unidentified events ("Name": "?") have no Reference at all.
        => allMissions.Where(m => m.Reference != null).GroupBy(m => m.Reference).Where(g => g.Count() > 1).Select(g => g.Key).Should().BeEmpty();

    [Test]
    public void Every_MissionRef_is_a_mission()
    {
        var missionRefs = allMissions.Select(m => m.Reference).ToHashSet();

        allArticles.Where(a => !string.IsNullOrEmpty(a.MissionRef) && !missionRefs.Contains(a.MissionRef))
                   .Select(a => $"{a.Reference} -> {a.MissionRef}")
                   .Should().BeEmpty();
    }

    [Test]
    public void Every_Related_reference_is_an_article_or_mission()
    {
        var known = allMissions.Select(m => m.Reference).Concat(allArticles.Select(a => a.Reference)).ToHashSet();

        var dangling = allArticles.SelectMany(a => (a.Related ?? Array.Empty<string>()).Select(r => (a.Reference, r)))
            .Concat(allMissions.SelectMany(m => (m.Related ?? Array.Empty<string>()).Select(r => (m.Reference, r))))
            .Where(x => !known.Contains(x.r))
            .Select(x => $"{x.Reference} -> {x.r}");

        dangling.Should().BeEmpty();
    }

    [Test]
    public void Every_ContentUrl_exists()
    {
        var urls = allArticles.Select(a => (a.Reference, a.ContentUrl))
            .Concat(allMissions.Select(m => (m.Reference, m.ContentUrl)))
            .Where(x => !string.IsNullOrEmpty(x.ContentUrl));

        urls.Where(x => !File.Exists(LocalPath(x.ContentUrl)))
            .Select(x => $"{x.Reference} -> {x.ContentUrl}")
            .Should().BeEmpty();
    }

    // ---- mission dates (CLAUDE.md: every real-world mission was a Fri-Sun weekend) ---------

    [Test]
    public void Pre_jump_missions_run_Friday_to_the_following_Sunday()
    {
        var wrong = allMissions
            .Where(m => m.EndDate.Year < 1997)
            .Where(m => m.StartDate.DayOfWeek != DayOfWeek.Friday || m.EndDate != m.StartDate.AddDays(2))
            .Select(m => $"{m.Reference}: {m.StartDate:ddd yyyy-MM-dd} - {m.EndDate:ddd yyyy-MM-dd}");

        // Joined, because FluentAssertions only names the first item of a non-empty collection.
        string.Join(Environment.NewLine, wrong).Should().BeEmpty();
    }

    // ---- change history --------------------------------------------------------------------

    [Test]
    public void Change_history_has_one_well_formed_entry_per_day_and_at_least_three_highlights()
    {
        var entries = ChangeHistoryService.Parse(File.ReadAllText(LocalPath(ChangeHistoryEntry.ContentUrl)));

        entries.Should().NotBeEmpty();
        var problems = entries
            .Where(e => string.IsNullOrEmpty(e.Id) || string.IsNullOrEmpty(e.Date) ||
                        string.IsNullOrEmpty(e.Title) || string.IsNullOrEmpty(e.SummaryHtml))
            .Select(e => $"'{e.Date} - {e.Title}' needs '## {{date}} - {{title}} {{#id}}' and a summary paragraph");
        string.Join(Environment.NewLine, problems).Should().BeEmpty();

        entries.Select(e => e.Id).Should().OnlyHaveUniqueItems();
        entries.Select(e => e.Date).Should().OnlyHaveUniqueItems("there's one entry per day");
        entries.Count(e => e.IsHighlight).Should().BeGreaterThanOrEqualTo(3);
    }

    // ---- helpers ---------------------------------------------------------------------------

    private static T Read<T>(string relative)
        => JsonSerializer.Deserialize<T>(File.ReadAllText(LocalPath(relative)), Strict);

    private static string LocalPath(string url)
        => Path.Combine(WwwRoot, Uri.UnescapeDataString(url.TrimStart('/')).Replace('/', Path.DirectorySeparatorChar));

    private static string FindWwwRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "web", "wwwroot");
            if (Directory.Exists(Path.Combine(candidate, "data")))
                return candidate;
        }
        throw new DirectoryNotFoundException("Could not find web/wwwroot above the test output directory.");
    }

    /// <summary>Serves files straight from wwwroot, like the static host does.</summary>
    private sealed class WwwRootHandler(string root) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = Path.Combine(root, Uri.UnescapeDataString(request.RequestUri!.AbsolutePath.TrimStart('/')));
            return File.Exists(path)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(await File.ReadAllBytesAsync(path, cancellationToken)) }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
