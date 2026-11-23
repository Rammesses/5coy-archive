using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Models;

using NUnit.Framework;

using Services;

using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Services;

public class ArticlesServiceTests
{
    // Sections:  ic -> ilof (nested), reports
    // Groups:    ic      : letter-1 (child: letter-1-appendix), letter-2 -> related [report-procyon, pegasus]
    //            ilof    : ilof-1 (from a Data file)
    //            reports : report-procyon (MissionRef procyon), report-pegasus (MissionRef pegasus,
    //                      child: report-pegasus-annex MissionRef pegasus)
    private StubHttpHandler http;
    private ArticlesService service;

    [SetUp]
    public void SetUp()
    {
        var sections = new[]
        {
            Section("ic", Section("ilof")),
            Section("reports"),
        };
        var groups = new[]
        {
            Group("ic",
                Article("letter-1", children: Article("letter-1-appendix")),
                Article("letter-2", related: new[] { "report-procyon", "pegasus" })),
            new ArticleGroup { Reference = "ilof", Data = "data/articles/ilof.json" },
            Group("reports",
                Article("report-procyon", missionRef: "procyon"),
                Article("report-pegasus", "pegasus", null, Article("report-pegasus-annex", missionRef: "pegasus"))),
        };

        http = new StubHttpHandler().ServeJson("data/articles/ilof.json", new[] { Article("ilof-1") });
        service = ArticlesService(sections, groups, http);
    }

    private static string[] Refs(Article[] articles) => articles.Select(a => a.Reference).ToArray();

    // ---- loading ---------------------------------------------------------------------------

    [Test]
    public async Task Articles_from_a_groups_Data_file_are_merged_into_the_group()
    {
        Refs(await service.GetItems("ilof")).Should().Equal("ilof-1");
    }

    [Test]
    public async Task Data_files_are_appended_after_inline_articles()
    {
        var sections = new[] { Section("ic") };
        var groups = new[] { new ArticleGroup { Reference = "ic", Articles = new[] { Article("inline") }, Data = "data/articles/ic.json" } };
        var stub = new StubHttpHandler().ServeJson("data/articles/ic.json", new[] { Article("from-file") });
        var svc = ArticlesService(sections, groups, stub);

        Refs(await svc.GetItems("ic")).Should().Equal("inline", "from-file");
    }

    [Test]
    public async Task Data_is_fetched_once_and_cached()
    {
        await service.GetSections();
        await service.GetItems("ic");
        await service.GetItem("reports", "report-procyon");
        await service.GetRelatedItems(await service.GetItem("ic", "letter-2"));

        http.RequestsFor("data/sections.json").Should().Be(1);
        http.RequestsFor("data/articles.json").Should().Be(1);
        http.RequestsFor("data/articles/ilof.json").Should().Be(1);
    }

    [Test]
    public async Task Child_articles_know_their_parent()
    {
        var appendix = await service.GetItem("ic", "letter-1-appendix");

        appendix.Parent.Should().NotBeNull();
        appendix.Parent.Reference.Should().Be("letter-1");
    }

    [Test]
    public async Task Top_level_articles_have_no_parent()
    {
        (await service.GetItem("ic", "letter-1")).Parent.Should().BeNull();
    }

    // ---- sections --------------------------------------------------------------------------

    [Test]
    public async Task GetSection_finds_nested_sections()
    {
        (await service.GetSection("ilof")).Reference.Should().Be("ilof");
    }

    [Test]
    public async Task Sub_sections_know_their_parent_section()
    {
        var ilof = await service.GetSection("ilof");

        ilof.Parent.Should().NotBeNull();
        ilof.Parent.Reference.Should().Be("ic");
        (await service.GetSection("ic")).Parent.Should().BeNull();
    }

    [Test]
    public async Task GetSection_returns_a_synthetic_section_for_all()
    {
        var all = await service.GetSection(Section<Article>.AllItemsRef);

        all.Reference.Should().Be(Section<Article>.AllItemsRef);
        all.Title.Should().Be("All Articles");
    }

    [Test]
    public async Task GetSection_returns_null_for_an_unknown_section()
    {
        (await service.GetSection("nope")).Should().BeNull();
    }

    // ---- items -----------------------------------------------------------------------------

    [Test]
    public async Task GetItems_returns_only_the_top_level_articles_of_a_section()
    {
        Refs(await service.GetItems("ic")).Should().Equal("letter-1", "letter-2");
    }

    [Test]
    public async Task GetItems_for_all_returns_every_article_including_children()
    {
        Refs(await service.GetItems(Section<Article>.AllItemsRef)).Should().BeEquivalentTo(
            "letter-1", "letter-1-appendix", "letter-2", "ilof-1",
            "report-procyon", "report-pegasus", "report-pegasus-annex");
    }

    [Test]
    public async Task GetItems_returns_empty_for_an_unknown_section()
    {
        (await service.GetItems("nope")).Should().BeEmpty();
    }

    [Test]
    public async Task GetItems_returns_empty_for_a_section_with_no_article_group()
    {
        var svc = ArticlesService(new[] { Section("empty") }, Array.Empty<ArticleGroup>());

        (await svc.GetItems("empty")).Should().BeEmpty();
    }

    [Test]
    public async Task GetAllItemsInTree_includes_children()
    {
        Refs(await service.GetAllItemsInTree("ic")).Should().BeEquivalentTo("letter-1", "letter-1-appendix", "letter-2");
    }

    [Test]
    public async Task GetAllItemsInTree_returns_empty_for_an_unknown_section()
    {
        (await service.GetAllItemsInTree("nope")).Should().BeEmpty();
    }

    [Test]
    public async Task GetItem_finds_a_child_article_within_its_section()
    {
        (await service.GetItem("reports", "report-pegasus-annex")).Reference.Should().Be("report-pegasus-annex");
    }

    [Test]
    public async Task GetItem_does_not_find_an_article_from_another_section()
    {
        (await service.GetItem("ic", "report-procyon")).Should().BeNull();
    }

    [Test]
    public async Task GetItem_returns_null_for_an_unknown_section()
    {
        (await service.GetItem("nope", "letter-1")).Should().BeNull();
    }

    [Test]
    public async Task GetItem_with_all_finds_top_level_articles_in_any_section()
    {
        (await service.GetItem(Section<Article>.AllItemsRef, "report-procyon")).Reference.Should().Be("report-procyon");
    }

    // ---- missions --------------------------------------------------------------------------

    [Test]
    public async Task GetItemsByMissionRef_returns_articles_reporting_on_the_mission()
    {
        Refs(await service.GetItemsByMissionRef("procyon")).Should().Equal("report-procyon");
    }

    [Test]
    public async Task GetItemsByMissionRef_only_searches_top_level_articles()
    {
        // Documented in CLAUDE.md: Children are not listed on the mission page.
        Refs(await service.GetItemsByMissionRef("pegasus")).Should().Equal("report-pegasus");
    }

    [Test]
    public async Task GetItemsByMissionRef_returns_empty_for_an_unknown_mission()
    {
        (await service.GetItemsByMissionRef("nope")).Should().BeEmpty();
    }

    // ---- related articles ------------------------------------------------------------------

    [Test]
    public async Task GetRelatedItems_includes_articles_this_one_links_to()
    {
        var letter2 = await service.GetItem("ic", "letter-2");

        Refs(await service.GetRelatedItems(letter2)).Should().Equal("report-procyon");
    }

    [Test]
    public async Task GetRelatedItems_includes_articles_that_link_back()
    {
        var report = await service.GetItem("reports", "report-procyon");

        Refs(await service.GetRelatedItems(report)).Should().Equal("letter-2");
    }

    [Test]
    public async Task GetRelatedItems_follows_links_to_and_from_child_articles()
    {
        var sections = new[] { Section("ic") };
        var groups = new[]
        {
            Group("ic",
                Article("parent", children: Article("child", related: new[] { "other" })),
                Article("other")),
        };
        var svc = ArticlesService(sections, groups);

        Refs(await svc.GetRelatedItems(await svc.GetItem("ic", "other"))).Should().Equal("child");
        Refs(await svc.GetRelatedItems(await svc.GetItem("ic", "child"))).Should().Equal("other");
    }

    [Test]
    public async Task GetRelatedItems_lists_an_article_linked_both_ways_once_and_never_itself()
    {
        var sections = new[] { Section("ic") };
        var groups = new[]
        {
            Group("ic",
                Article("a", related: new[] { "a", "b" }),
                Article("b", related: new[] { "a" })),
        };
        var svc = ArticlesService(sections, groups);

        Refs(await svc.GetRelatedItems(await svc.GetItem("ic", "a"))).Should().Equal("b");
    }

    [Test]
    public async Task GetRelatedItems_returns_empty_for_null()
    {
        (await service.GetRelatedItems(null)).Should().BeEmpty();
    }

    [Test]
    public async Task GetRelatedItemsForMission_includes_articles_the_mission_links_to()
    {
        var mission = Mission("pegasus", "letter-1", "report-procyon");

        Refs(await service.GetRelatedItemsForMission(mission)).Should().Contain(new[] { "letter-1", "report-procyon" });
    }

    [Test]
    public async Task GetRelatedItemsForMission_includes_articles_that_name_the_mission()
    {
        Refs(await service.GetRelatedItemsForMission(Mission("pegasus"))).Should().Equal("letter-2");
    }

    [Test]
    public async Task GetRelatedItemsForMission_leaves_out_articles_already_reporting_on_the_mission()
    {
        // report-pegasus is listed via MissionRef already; naming it in Related mustn't list it twice.
        var mission = Mission("pegasus", "report-pegasus", "report-pegasus-annex");

        Refs(await service.GetRelatedItemsForMission(mission)).Should().Equal("letter-2");
    }

    [Test]
    public async Task GetRelatedItemsForMission_returns_empty_for_null()
    {
        (await service.GetRelatedItemsForMission(null)).Should().BeEmpty();
    }

    // ---- section paths (breadcrumbs) ---------------------------------------------------------

    [Test]
    public async Task GetSectionForItem_finds_the_section_holding_a_child_article()
    {
        (await service.GetSectionForItem("letter-1-appendix")).Reference.Should().Be("ic");
    }

    [Test]
    public async Task GetSectionForItem_returns_null_for_an_unknown_article()
    {
        (await service.GetSectionForItem("nope")).Should().BeNull();
    }

    [Test]
    public async Task GetSectionPathForItem_returns_the_full_path_to_a_nested_section()
    {
        var article = await service.GetItem("ilof", "ilof-1");

        var path = await service.GetSectionPathForItem(article);

        path.Select(s => s.Reference).Should().Equal("ic", "ilof");
    }

    [Test]
    public async Task GetSectionPathForItem_prefers_the_group_holding_that_exact_instance()
    {
        // The same report appears in both groups (as in In-Character and Mission Reports).
        var sections = new[] { Section("ic"), Section("reports") };
        var groups = new[]
        {
            Group("ic", Article("report")),
            Group("reports", Article("report")),
        };
        var svc = ArticlesService(sections, groups);

        var inReports = await svc.GetItem("reports", "report");
        var inIc = await svc.GetItem("ic", "report");

        (await svc.GetSectionPathForItem(inReports)).Select(s => s.Reference).Should().Equal("reports");
        (await svc.GetSectionPathForItem(inIc)).Select(s => s.Reference).Should().Equal("ic");
    }

    [Test]
    public async Task GetSectionPathForItem_falls_back_to_matching_by_reference()
    {
        var copy = Article("report-procyon");

        (await service.GetSectionPathForItem(copy)).Select(s => s.Reference).Should().Equal("reports");
    }

    [Test]
    public async Task GetSectionPathForItem_returns_empty_for_null_or_unknown_articles()
    {
        (await service.GetSectionPathForItem(null)).Should().BeEmpty();
        (await service.GetSectionPathForItem(Article("nope"))).Should().BeEmpty();
    }
}
