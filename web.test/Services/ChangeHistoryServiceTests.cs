using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Services;

using web.test.TestSupport;

namespace web.test.Services;

public class ChangeHistoryServiceTests
{
    private const string Sample = """
        # Change History

        Intro paragraph.

        <!-- format notes -->

        ## 25 November 2026 - Videos {#2026-11-25-videos .whats-new}

        A new [Videos](/videos) page.

        ## 24 November 2026 - Minor fix {#2026-11-24-fix}

        Small *thing*.

        ## October 2025 - Big - with a dash {#2025-10-big .whats-new}

        First paragraph.

        Second paragraph.

        ## 2 October 2025 - Another {#2025-10-02-another .whats-new}

        A.

        ## 1 October 2025 - Oldest {#2025-10-01-oldest .whats-new}

        B.
        """;

    [Test]
    public void Parses_each_level_two_heading_as_an_entry_in_document_order()
    {
        var entries = ChangeHistoryService.Parse(Sample);

        entries.Select(e => e.Id).Should().Equal(
            "2026-11-25-videos", "2026-11-24-fix", "2025-10-big", "2025-10-02-another", "2025-10-01-oldest");
        entries[0].Date.Should().Be("25 November 2026");
        entries[0].Title.Should().Be("Videos");
        entries[0].Url.Should().Be("/change-history#2026-11-25-videos");
        entries[0].SummaryHtml.Should().Be("A new <a href=\"/videos\">Videos</a> page.");
    }

    [Test]
    public void Splits_the_date_at_the_first_dash_only_and_takes_only_the_first_paragraph()
    {
        var big = ChangeHistoryService.Parse(Sample)[2];

        big.Date.Should().Be("October 2025");
        big.Title.Should().Be("Big - with a dash");
        big.SummaryHtml.Should().Be("First paragraph.");
    }

    [Test]
    public void Only_entries_marked_whats_new_are_highlights()
    {
        ChangeHistoryService.Parse(Sample).Where(e => e.IsHighlight).Select(e => e.Title)
            .Should().Equal("Videos", "Big - with a dash", "Another", "Oldest");
    }

    [Test]
    public async Task Highlights_are_the_newest_three_marked_entries()
    {
        var http = new StubHttpHandler();
        http["/change-history.md"] = Sample;
        var service = new ChangeHistoryService(http.CreateClient(), NullLogger<ChangeHistoryService>.Instance);

        (await service.GetHighlights(3)).Select(e => e.Title).Should().Equal("Videos", "Big - with a dash", "Another");
    }

    [Test]
    public async Task Loads_the_file_once()
    {
        var http = new StubHttpHandler();
        http["/change-history.md"] = Sample;
        var service = new ChangeHistoryService(http.CreateClient(), NullLogger<ChangeHistoryService>.Instance);

        await service.GetHighlights();
        await service.GetEntries();

        http.RequestsFor("/change-history.md").Should().Be(1);
    }

    [Test]
    public async Task A_missing_file_gives_no_entries()
    {
        var http = new StubHttpHandler();
        var service = new ChangeHistoryService(http.CreateClient(), NullLogger<ChangeHistoryService>.Instance);

        (await service.GetHighlights()).Should().BeEmpty();
    }
}
