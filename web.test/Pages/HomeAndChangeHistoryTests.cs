using System.Linq;

using Bunit;

using FluentAssertions;

using Models;

using NUnit.Framework;

using web.test.TestSupport;

namespace web.test.Pages;

public class HomePageTests : ComponentTestBase
{
    [Test]
    public void Shows_whats_new_with_links_into_the_change_history()
    {
        ChangeHistory.Setup(c => c.GetHighlights(3)).ReturnsAsync(new[]
        {
            new ChangeHistoryEntry { Id = "2026-11-25-videos", Date = "25 November 2026", Title = "Videos", SummaryHtml = "A <a href=\"/videos\">page</a>.", IsHighlight = true },
            new ChangeHistoryEntry { Id = "2026-11-23-interstella", Date = "23 November 2026", Title = "Interstella!", SummaryHtml = "More issues.", IsHighlight = true },
        });

        var cut = Ctx.Render<web.Pages.Index>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("ul.whats-new .whats-new-heading a").Select(a => a.GetAttribute("href"))
                .Should().Equal("/change-history#2026-11-25-videos", "/change-history#2026-11-23-interstella");
            cut.Find("ul.whats-new .whats-new-date").TextContent.Should().Be("25 November 2026");
            cut.Find("ul.whats-new .whats-new-summary a").GetAttribute("href").Should().Be("/videos");
            cut.Find("a.whats-new-all").GetAttribute("href").Should().Be("/change-history");
        });
    }

    [Test]
    public void Still_links_to_the_change_history_when_there_are_no_highlights()
    {
        var cut = Ctx.Render<web.Pages.Index>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("ul.whats-new").Should().BeEmpty();
            cut.Find("a.whats-new-all").GetAttribute("href").Should().Be("/change-history");
        });
    }
}

public class ChangeHistoryPageTests : ComponentTestBase
{
    [Test]
    public void Renders_the_change_history_markdown_with_entry_ids()
    {
        Http["/change-history.md"] = "# Change History\n\n## 25 November 2026 - Videos {#2026-11-25-videos .whats-new}\n\nA new page.\n";

        var cut = Ctx.Render<web.Pages.ChangeHistory>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Be("Change History");
            var h2 = cut.Find("h2");
            h2.Id.Should().Be("2026-11-25-videos");
            h2.TextContent.Should().Be("25 November 2026 - Videos");
        });
    }
}
