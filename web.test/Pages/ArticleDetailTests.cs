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
            var missionLink = cut.FindAll("h5 a").Single(a => a.GetAttribute("href") == "/mission/procyon");
            missionLink.ParentElement!.TextContent.Should().Contain("Mission Procyon").And.Contain("Oct 2492");
        });
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
