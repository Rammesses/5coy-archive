using System;
using System.Linq;
using System.Threading.Tasks;

using Bunit;

using FluentAssertions;

using Models;

using Moq;

using NUnit.Framework;

using web.Pages;
using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Pages;

/// <summary>
/// The router reuses the same MissionDetail instance when navigating from one /mission/{ref}
/// URL to another (e.g. a link from Procyon's summary to Pegasus), only changing MissionRef.
/// These tests drive that by re-rendering the page with a new parameter.
/// </summary>
public class MissionDetailTests : ComponentTestBase
{
    private static readonly Mission Procyon = new()
    {
        Reference = "2492-10_mission-procyon",
        Name = "Mission Procyon",
        EndDate = new DateTime(1992, 10, 18),
        ContentUrl = "/content/missions/procyon.md",
        Related = new[] { "2494-06_mission-pegasus" },
    };

    private static readonly Mission Pegasus = new()
    {
        Reference = "2494-06_mission-pegasus",
        Name = "Mission Pegasus",
        EndDate = new DateTime(1994, 6, 12),
        ContentUrl = "/content/missions/pegasus.md",
        Related = new[] { "2492-10_mission-procyon" },
    };

    [SetUp]
    public void SetUp()
    {
        Missions.Setup(m => m.GetMissionByRef(Procyon.Reference)).ReturnsAsync(Procyon);
        Missions.Setup(m => m.GetMissionByRef(Pegasus.Reference)).ReturnsAsync(Pegasus);
        Missions.Setup(m => m.GetRelatedMissions(Procyon.Reference, It.IsAny<string[]>())).ReturnsAsync(new[] { Pegasus });
        Missions.Setup(m => m.GetRelatedMissions(Pegasus.Reference, It.IsAny<string[]>())).ReturnsAsync(new[] { Procyon });

        Http[Procyon.ContentUrl] = "Procyon summary. See [Pegasus](/mission/2494-06_mission-pegasus).";
        Http[Pegasus.ContentUrl] = "Pegasus summary. See [Procyon](/mission/2492-10_mission-procyon).";
    }

    private IRenderedComponent<MissionDetail> RenderMission(Mission mission)
        => Ctx.Render<MissionDetail>(p => p.Add(c => c.MissionRef, mission.Reference));

    [Test]
    public void Renders_the_requested_mission()
    {
        var cut = RenderMission(Procyon);

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("Mission Procyon");
            cut.Find(".mission-summary").TextContent.Should().Contain("Procyon summary");
        });
    }

    [Test]
    public void Lists_reports_and_related_articles_together()
    {
        var report = Article("report-procyon", "2492-10_mission-procyon");
        var newsletter = Article("newsletter");
        Articles.Setup(a => a.GetItemsByMissionRef(Procyon.Reference)).ReturnsAsync(new[] { report });
        Articles.Setup(a => a.GetRelatedItemsForMission(Procyon)).ReturnsAsync(new[] { newsletter });

        var cut = RenderMission(Procyon);

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Title of report-procyon");
            cut.Markup.Should().Contain("Title of newsletter");
        });
    }

    [Test]
    public void Has_a_breadcrumb_back_to_the_missions_page()
    {
        var cut = RenderMission(Procyon);

        cut.WaitForAssertion(() =>
        {
            var back = cut.Find("h5.page-nav a");
            back.GetAttribute("href").Should().Be("/missions");
            back.TextContent.Trim().Should().Be("Missions");
        });
    }

    [Test]
    public void Still_has_the_breadcrumb_while_loading_or_for_an_unknown_mission()
    {
        var cut = Ctx.Render<MissionDetail>(p => p.Add(c => c.MissionRef, "nope"));

        cut.WaitForAssertion(() => cut.Find("h5.page-nav a").GetAttribute("href").Should().Be("/missions"));
    }

    [Test]
    public void Related_missions_are_shown_as_IC_Missions_breadcrumbs_with_the_date_in_the_link()
    {
        var cut = RenderMission(Procyon);

        cut.WaitForAssertion(() =>
        {
            var links = cut.FindAll("li.related-mission a").Select(a => (a.TextContent, a.GetAttribute("href")));
            links.Should().Equal(
                ("IC", "/section/in-character"),
                ("Missions", "/missions"),
                ("Mission Pegasus (Jun 2494)", "/mission/2494-06_mission-pegasus"));
        });
    }

    [Test]
    public void An_unknown_mission_shows_the_loading_placeholder()
    {
        var cut = Ctx.Render<MissionDetail>(p => p.Add(c => c.MissionRef, "nope"));

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("nope");
            cut.FindAll(".mission-summary").Should().BeEmpty();
        });
    }

    [Test]
    public void A_summary_that_fails_to_load_stays_hidden()
    {
        var noFile = new Mission { Reference = "m", Name = "Mission M", ContentUrl = "/content/missions/missing.md" };
        Missions.Setup(m => m.GetMissionByRef("m")).ReturnsAsync(noFile);

        var cut = Ctx.Render<MissionDetail>(p => p.Add(c => c.MissionRef, "m"));

        cut.WaitForAssertion(() => cut.Find(".mission-summary").ClassList.Should().Contain("collapse"));
    }

    [Test]
    public void Navigating_to_another_mission_reloads_the_page_content()
    {
        var cut = RenderMission(Procyon);
        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Mission Procyon"));

        // Same component instance, new route parameter: what the router does for /mission -> /mission.
        cut.Render(p => p.Add(c => c.MissionRef, Pegasus.Reference));

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("Mission Pegasus");
            cut.Find(".mission-summary").TextContent.Should().Contain("Pegasus summary");
            cut.Find(".mission-summary").TextContent.Should().NotContain("Procyon summary");
        });
    }

    [Test]
    public void Navigating_to_another_mission_reloads_the_related_list()
    {
        var cut = RenderMission(Procyon);
        cut.WaitForAssertion(() =>
            cut.Find("li.related-mission a[href^='/mission/']").GetAttribute("href").Should().Be($"/mission/{Pegasus.Reference}"));

        cut.Render(p => p.Add(c => c.MissionRef, Pegasus.Reference));

        cut.WaitForAssertion(() =>
            cut.Find("li.related-mission a[href^='/mission/']").GetAttribute("href").Should().Be($"/mission/{Procyon.Reference}"));
    }

    [Test]
    public void Navigating_to_a_mission_without_a_summary_shows_no_stale_summary()
    {
        var noSummary = new Mission { Reference = "2493-01_mission-unknown", Name = "Mission Unknown" };
        Missions.Setup(m => m.GetMissionByRef(noSummary.Reference)).ReturnsAsync(noSummary);

        var cut = RenderMission(Procyon);
        cut.WaitForAssertion(() => cut.Find(".mission-summary").TextContent.Should().Contain("Procyon summary"));

        cut.Render(p => p.Add(c => c.MissionRef, noSummary.Reference));

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("Mission Unknown");
            cut.FindAll(".mission-summary").Should().BeEmpty();
            cut.FindAll(".related-breadcrumbs").Should().BeEmpty();
        });
    }

    [Test]
    public void Each_summary_is_fetched_once_per_visit()
    {
        var cut = RenderMission(Procyon);
        cut.WaitForAssertion(() => cut.Find(".mission-summary").TextContent.Should().Contain("Procyon summary"));
        cut.Render(p => p.Add(c => c.MissionRef, Pegasus.Reference));
        cut.WaitForAssertion(() => cut.Find(".mission-summary").TextContent.Should().Contain("Pegasus summary"));

        Http.RequestsFor(Procyon.ContentUrl).Should().Be(1);
        Http.RequestsFor(Pegasus.ContentUrl).Should().Be(1);
    }

    [Test]
    public void A_slow_load_for_the_previous_mission_does_not_overwrite_the_current_one()
    {
        var slowProcyon = new TaskCompletionSource<Mission>();
        Missions.Setup(m => m.GetMissionByRef(Procyon.Reference)).Returns(slowProcyon.Task);

        var cut = RenderMission(Procyon);
        cut.Render(p => p.Add(c => c.MissionRef, Pegasus.Reference));
        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Mission Pegasus"));

        // Procyon's lookup finishes after the user has already moved on to Pegasus.
        cut.InvokeAsync(() => slowProcyon.SetResult(Procyon));

        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Mission Pegasus"));
        cut.Find(".mission-summary").TextContent.Should().Contain("Pegasus summary");
    }
}
