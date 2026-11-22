using System;
using System.Linq;

using Bunit;

using FluentAssertions;

using Models;

using Moq;

using NUnit.Framework;

using web.Controls;
using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Controls;

public class RelatedArticlesBreadcrumbListTests : ComponentTestBase
{
    [Test]
    public void Shows_the_section_trail_then_the_article()
    {
        var ic = new Section<Article> { Reference = "ic", Title = "In-Character", ShortTitle = "IC" };
        var reports = new Section<Article> { Reference = "reports", Title = "Mission Reports" };
        var report = Article("report-procyon");
        Articles.Setup(a => a.GetSectionPathForItem(report)).ReturnsAsync(new[] { ic, reports });

        var cut = Ctx.Render<RelatedArticlesBreadcrumbList>(p => p.Add(c => c.Articles, new[] { report }));

        cut.WaitForAssertion(() =>
        {
            var links = cut.FindAll("li a").Select(a => (a.TextContent, a.GetAttribute("href"))).ToArray();
            links.Should().Equal(
                ("IC", "/section/ic"),
                ("Mission Reports", "/section/reports"),
                ("Title of report-procyon", "/article/reports/report-procyon"));
        });
    }

    [Test]
    public void Includes_parent_articles_in_the_trail()
    {
        var reports = new Section<Article> { Reference = "reports", Title = "Mission Reports" };
        var parent = Article("newsletter");
        var child = Article("newsletter-p2");
        child.Parent = parent;
        Articles.Setup(a => a.GetSectionPathForItem(child)).ReturnsAsync(new[] { reports });

        var cut = Ctx.Render<RelatedArticlesBreadcrumbList>(p => p.Add(c => c.Articles, new[] { child }));

        cut.WaitForAssertion(() =>
            cut.FindAll("li a").Select(a => a.GetAttribute("href")).Should().Equal(
                "/section/reports", "/article/reports/newsletter", "/article/reports/newsletter-p2"));
    }

    [Test]
    public void Falls_back_to_the_all_section_when_the_article_has_no_section()
    {
        var cut = Ctx.Render<RelatedArticlesBreadcrumbList>(p => p.Add(c => c.Articles, new[] { Article("orphan") }));

        cut.WaitForAssertion(() =>
            cut.Find("li a").GetAttribute("href").Should().Be("/article/all/orphan"));
    }

    [Test]
    public void An_article_without_a_reference_is_plain_text()
    {
        var cut = Ctx.Render<RelatedArticlesBreadcrumbList>(p => p.Add(c => c.Articles, new[] { new Article { Title = "Untitled scrap" } }));

        cut.WaitForAssertion(() => cut.Find("li").TextContent.Should().Contain("Untitled scrap"));
        cut.FindAll("li a").Should().BeEmpty();
    }

    [Test]
    public void Shows_a_download_link_when_the_article_has_a_pdf()
    {
        var article = Article("with-pdf");
        article.PdfUrl = "/media/with-pdf.pdf";

        var cut = Ctx.Render<RelatedArticlesBreadcrumbList>(p => p.Add(c => c.Articles, new[] { article }));

        cut.WaitForAssertion(() =>
            cut.Find("a[title='Download as PDF']").GetAttribute("href").Should().Be("/media/with-pdf.pdf"));
    }

    [Test]
    public void Handles_null_articles()
    {
        var cut = Ctx.Render<RelatedArticlesBreadcrumbList>(p => p.Add(c => c.Articles, (Article[])null));

        cut.FindAll("li").Should().BeEmpty();
    }
}

public class PdfDownloadButtonTests : ComponentTestBase
{
    [Test]
    public void Renders_nothing_without_a_pdf()
        => Ctx.Render<PdfDownloadButton>(p => p.Add(c => c.PdfUrl, null)).Markup.Trim().Should().BeEmpty();

    [Test]
    public void Links_straight_to_the_pdf_when_no_media_base_url_is_configured()
        => Ctx.Render<PdfDownloadButton>(p => p.Add(c => c.PdfUrl, "/media/a.pdf"))
              .Find("a").GetAttribute("href").Should().Be("/media/a.pdf");

    [Test]
    public void Resolves_the_pdf_against_the_media_base_url()
    {
        Config["Media:BaseUrl"] = "https://cdn.example.org/";

        Ctx.Render<PdfDownloadButton>(p => p.Add(c => c.PdfUrl, "/media/a.pdf"))
           .Find("a").GetAttribute("href").Should().Be("https://cdn.example.org/media/a.pdf");
    }
}

public class MissionTableRowTests : ComponentTestBase
{
    private static readonly Mission Pegasus = new()
    {
        Reference = "2494-06_mission-pegasus",
        Name = "Mission Pegasus",
        EndDate = new DateTime(1994, 6, 12),
        Planet = "Kerensky",
        Location = "Hill 7",
    };

    private IRenderedComponent<MissionTableRow> RenderRow(bool expanded = false, Action onToggle = null)
        => Ctx.Render<MissionTableRow>(p => p
            .Add(c => c.Mission, Pegasus)
            .Add(c => c.IsExpanded, expanded)
            .Add(c => c.OnToggle, onToggle ?? (() => { })));

    [Test]
    public void Shows_the_in_game_date_name_link_and_location()
    {
        var cells = RenderRow().FindAll("tr")[0].QuerySelectorAll("td");

        cells[0].TextContent.Should().Be("Jun 2494");
        cells[1].QuerySelector("a")!.GetAttribute("href").Should().Be("/mission/2494-06_mission-pegasus");
        cells[1].TextContent.Should().Be("Mission Pegasus");
        cells[2].TextContent.Should().Be("Kerensky (Hill 7)");
    }

    [Test]
    public void A_mission_without_a_reference_is_not_a_link()
    {
        var cut = Ctx.Render<MissionTableRow>(p => p.Add(c => c.Mission, new Mission { Name = "Nameless" }));

        cut.FindAll("tr")[0].QuerySelectorAll("td")[1].QuerySelector("a").Should().BeNull();
    }

    [Test]
    public void The_toggle_button_is_hidden_when_there_is_nothing_to_show()
    {
        RenderRow().Find("button").ClassList.Should().Contain("collapse");
    }

    [Test]
    public void The_toggle_button_shows_when_the_mission_has_reports()
    {
        Articles.Setup(a => a.GetItemsByMissionRef(Pegasus.Reference)).ReturnsAsync(new[] { Article("report-pegasus") });

        var cut = RenderRow();

        cut.WaitForAssertion(() => cut.Find("button").ClassList.Should().NotContain("collapse"));
    }

    [Test]
    public void The_info_row_follows_IsExpanded()
    {
        RenderRow(expanded: false).FindAll("tr")[1].ClassList.Should().Contain("collapse");

        var expanded = RenderRow(expanded: true);
        expanded.FindAll("tr")[1].ClassList.Should().NotContain("collapse");
        expanded.Find("button").GetAttribute("aria-expanded").Should().Be("true");
    }

    [Test]
    public void Clicking_the_button_raises_OnToggle()
    {
        var toggled = 0;
        var cut = RenderRow(onToggle: () => toggled++);

        cut.Find("button").Click();

        toggled.Should().Be(1);
    }
}
