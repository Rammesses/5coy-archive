using System;
using System.Linq;
using System.Threading.Tasks;

using Bunit;

using FluentAssertions;

using Models;

using Moq;

using NUnit.Framework;

using web.Controls;
using web.Pages;
using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Pages;

/// <summary>Article page, including article -> article navigation (same component instance).</summary>
public class ArticleDetailTests : ComponentTestBase
{
    private Section<Article> reports;
    private Article reportProcyon;
    private Article reportPegasus;
    private Mission procyon;

    [SetUp]
    public void SetUp()
    {
        // PdfViewButton wraps a BlazorBootstrap modal + PDF viewer that needs real JS.
        Ctx.ComponentFactories.AddStub<PdfViewButton>();

        reports = new Section<Article> { Reference = "reports", Title = "Mission Reports", Glyph = "oi-flag" };
        reportProcyon = Article("report-procyon", "procyon", new[] { "report-pegasus" });
        reportProcyon.ContentUrl = "/content/reports/procyon.md";
        reportPegasus = Article("report-pegasus");
        reportPegasus.ContentUrl = "/content/reports/pegasus.md";
        procyon = new Mission { Reference = "procyon", Name = "Mission Procyon", EndDate = new DateTime(1992, 10, 18) };

        Articles.Setup(a => a.GetSection("reports")).ReturnsAsync(reports);
        Articles.Setup(a => a.GetAllItemsInTree("reports")).ReturnsAsync(new[] { reportProcyon, reportPegasus });
        Articles.Setup(a => a.GetItem("reports", "report-procyon")).ReturnsAsync(reportProcyon);
        Articles.Setup(a => a.GetItem("reports", "report-pegasus")).ReturnsAsync(reportPegasus);
        Articles.Setup(a => a.GetRelatedItems(reportProcyon)).ReturnsAsync(new[] { reportPegasus });
        Articles.Setup(a => a.GetRelatedItems(reportPegasus)).ReturnsAsync(new[] { reportProcyon });
        Missions.Setup(m => m.GetMissionByRef("procyon")).ReturnsAsync(procyon);

        Http[reportProcyon.ContentUrl] = "Procyon report text.";
        Http[reportPegasus.ContentUrl] = "Pegasus report text.";
    }

    private IRenderedComponent<ArticleDetail> RenderArticle(string section, string article)
        => Ctx.Render<ArticleDetail>(p => p.Add(c => c.SectionRef, section).Add(c => c.ArticleRef, article));

    [Test]
    public void Renders_the_article_with_its_section_link_and_content()
    {
        var cut = RenderArticle("reports", "report-procyon");

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("Title of report-procyon");
            cut.Find("h5 a").GetAttribute("href").Should().Be("/section/reports");
            cut.Markup.Should().Contain("Procyon report text.");
        });
    }

    [Test]
    public void Links_to_the_mission_it_reports_on()
    {
        var cut = RenderArticle("reports", "report-procyon");

        cut.WaitForAssertion(() =>
        {
            var mission = cut.Find("h5.page-nav .page-mission");
            mission.QuerySelector("a")!.GetAttribute("href").Should().Be("/mission/procyon");
            mission.TextContent.Should().Contain("Mission Procyon").And.Contain("October 2492");
        });
    }

    [Test]
    public void The_mission_link_shares_the_breadcrumb_line()
    {
        var cut = RenderArticle("reports", "report-procyon");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("h5.page-nav").Should().HaveCount(1);
            var nav = cut.Find("h5.page-nav");
            nav.QuerySelector(".page-breadcrumbs a")!.GetAttribute("href").Should().Be("/section/reports");
            // Breadcrumbs first, mission last: the CSS pushes the last item to the right.
            nav.Children.Select(c => c.ClassName).Should().Equal("page-breadcrumbs", "page-mission");
        });
    }

    [Test]
    public void Articles_without_a_mission_have_no_mission_link()
    {
        var cut = RenderArticle("reports", "report-pegasus");

        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Title of report-pegasus"));
        cut.FindAll(".page-mission").Should().BeEmpty();
    }

    [Test]
    public void Links_to_its_parent_article()
    {
        var child = Article("report-procyon-p2");
        child.Parent = reportProcyon;
        Articles.Setup(a => a.GetItem("reports", "report-procyon-p2")).ReturnsAsync(child);

        var cut = RenderArticle("reports", "report-procyon-p2");

        cut.WaitForAssertion(() =>
            cut.FindAll("h5 a").Select(a => a.GetAttribute("href")).Should().Contain("/article/reports/report-procyon"));
    }

    [Test]
    public void Lists_related_missions_and_articles()
    {
        var pegasus = new Mission { Reference = "pegasus", Name = "Mission Pegasus", EndDate = new DateTime(1994, 6, 12) };
        Missions.Setup(m => m.GetRelatedMissions("report-procyon", It.IsAny<string[]>())).ReturnsAsync(new[] { pegasus });

        var cut = RenderArticle("reports", "report-procyon");

        cut.WaitForAssertion(() =>
        {
            var related = cut.Find(".related-articles");
            related.QuerySelectorAll("a").Select(a => a.GetAttribute("href"))
                   .Should().Contain(new[] { "/mission/pegasus", "/article/all/report-pegasus" });
            related.QuerySelector("li.related-mission")!.TextContent
                   .Should().MatchRegex(@"IC\s*→\s*Missions\s*→\s*Mission Pegasus - June 2494");
        });
    }

    [Test]
    public void Child_articles_are_listed_with_the_related_articles_and_missions_in_one_list()
    {
        var briefing = Article("report-procyon-intel");
        briefing.Title = "Intelligence Briefing Notes";
        briefing.Parent = reportProcyon;
        reportProcyon.Children = new[] { briefing };
        var pegasus = new Mission { Reference = "pegasus", Name = "Mission 'Pegasus'", EndDate = new DateTime(1994, 6, 12) };
        Missions.Setup(m => m.GetRelatedMissions("report-procyon", It.IsAny<string[]>())).ReturnsAsync(new[] { pegasus });

        var cut = RenderArticle("reports", "report-procyon");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("h5").Select(h => h.TextContent.Trim()).Where(t => t.StartsWith("Related")).Should().Equal("Related:");
            cut.FindAll(".related-articles ul.related-breadcrumbs").Should().HaveCount(1);
            cut.FindAll(".related-articles li").Select(li => li.QuerySelectorAll("a").Last().TextContent).Should().Equal(
                "Mission 'Pegasus' - June 2494",           // IC → Missions → ...
                "Title of report-pegasus",                // Title of report-pegasus
                "Intelligence Briefing Notes");           // Title of report-procyon → Intelligence...
        });
    }

    [Test]
    public void An_article_with_no_transcription_shows_no_empty_content_block()
    {
        reportPegasus.ContentUrl = null;

        var cut = RenderArticle("reports", "report-pegasus");

        cut.WaitForAssertion(() => cut.Find(".article-content").ClassList.Should().Contain("collapse"));
    }

    [Test]
    public void An_article_with_a_transcription_shows_it()
    {
        var cut = RenderArticle("reports", "report-procyon");

        cut.WaitForAssertion(() =>
        {
            cut.Find(".article-content").ClassList.Should().NotContain("collapse");
            cut.Find(".article-content").TextContent.Should().Contain("Procyon report text.");
        });
    }

    [Test]
    public void An_unknown_article_lists_everything_in_the_section()
    {
        var cut = RenderArticle("reports", "nope");

        cut.WaitForAssertion(() =>
        {
            cut.Find(".loading").Should().NotBeNull();
            cut.FindAll("li").Select(li => li.TextContent).Should().Equal(
                "report-procyon - Title of report-procyon",
                "report-pegasus - Title of report-pegasus");
        });
    }

    [Test]
    public void Navigating_to_another_article_reloads_the_page()
    {
        var cut = RenderArticle("reports", "report-procyon");
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Procyon report text."));

        cut.Render(p => p.Add(c => c.SectionRef, "reports").Add(c => c.ArticleRef, "report-pegasus"));

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("Title of report-pegasus");
            cut.Markup.Should().Contain("Pegasus report text.");
            cut.Markup.Should().NotContain("Procyon report text.");
            cut.FindAll("a[href='/mission/procyon']").Should().BeEmpty();
        });
    }

    [Test]
    public void A_slow_load_for_the_previous_article_does_not_overwrite_the_current_one()
    {
        var slowProcyon = new TaskCompletionSource<Article>();
        Articles.Setup(a => a.GetItem("reports", "report-procyon")).Returns(slowProcyon.Task);

        var cut = RenderArticle("reports", "report-procyon");
        cut.Render(p => p.Add(c => c.SectionRef, "reports").Add(c => c.ArticleRef, "report-pegasus"));
        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Title of report-pegasus"));

        cut.InvokeAsync(() => slowProcyon.SetResult(reportProcyon));

        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Title of report-pegasus"));
        cut.Markup.Should().Contain("Pegasus report text.");
    }
}
