using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Bunit;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Models;

using Moq;

using NUnit.Framework;

using Services;

using web.Pages;

namespace web.test.Pages;

/// <summary>
/// The router reuses the same MissionDetail instance when navigating from one /mission/{ref}
/// URL to another (e.g. a link from Procyon's summary to Pegasus), only changing MissionRef.
/// These tests drive that by re-rendering the page with a new parameter.
/// </summary>
public class MissionDetailTests
{
    private BunitContext ctx;
    private Mock<IMissionsService> missions;
    private Mock<IArticlesService> articles;
    private StubContentHandler content;

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
        ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        missions = new Mock<IMissionsService>();
        missions.Setup(m => m.GetMissionByRef(Procyon.Reference)).ReturnsAsync(Procyon);
        missions.Setup(m => m.GetMissionByRef(Pegasus.Reference)).ReturnsAsync(Pegasus);
        missions.Setup(m => m.GetRelatedMissions(Procyon.Reference, It.IsAny<string[]>()))
                .ReturnsAsync(new[] { Pegasus });
        missions.Setup(m => m.GetRelatedMissions(Pegasus.Reference, It.IsAny<string[]>()))
                .ReturnsAsync(new[] { Procyon });

        articles = new Mock<IArticlesService>();
        articles.Setup(a => a.GetItemsByMissionRef(It.IsAny<string>())).ReturnsAsync(Array.Empty<Article>());
        articles.Setup(a => a.GetRelatedItemsForMission(It.IsAny<Mission>())).ReturnsAsync(Array.Empty<Article>());

        content = new StubContentHandler
        {
            [Procyon.ContentUrl] = "Procyon summary. See [Pegasus](/mission/2494-06_mission-pegasus).",
            [Pegasus.ContentUrl] = "Pegasus summary. See [Procyon](/mission/2492-10_mission-procyon).",
        };

        ctx.Services.AddSingleton(missions.Object);
        ctx.Services.AddSingleton(articles.Object);
        ctx.Services.AddSingleton(Mock.Of<IMarkdownService>());
        ctx.Services.AddSingleton(new HttpClient(content) { BaseAddress = new Uri("http://localhost/") });
    }

    [TearDown]
    public void TearDown() => ctx.Dispose();

    private IRenderedComponent<MissionDetail> RenderMission(Mission mission)
        => ctx.Render<MissionDetail>(p => p.Add(c => c.MissionRef, mission.Reference));

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
            cut.Find(".related-breadcrumbs a").GetAttribute("href").Should().Be($"/mission/{Pegasus.Reference}"));

        cut.Render(p => p.Add(c => c.MissionRef, Pegasus.Reference));

        cut.WaitForAssertion(() =>
            cut.Find(".related-breadcrumbs a").GetAttribute("href").Should().Be($"/mission/{Procyon.Reference}"));
    }

    [Test]
    public void Navigating_to_a_mission_without_a_summary_shows_no_stale_summary()
    {
        var noSummary = new Mission { Reference = "2493-01_mission-unknown", Name = "Mission Unknown" };
        missions.Setup(m => m.GetMissionByRef(noSummary.Reference)).ReturnsAsync(noSummary);
        missions.Setup(m => m.GetRelatedMissions(noSummary.Reference, It.IsAny<string[]>()))
                .ReturnsAsync(Array.Empty<Mission>());

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

        content.RequestCount.Should().Be(2);
    }

    [Test]
    public void A_slow_load_for_the_previous_mission_does_not_overwrite_the_current_one()
    {
        var slowProcyon = new TaskCompletionSource<Mission>();
        missions.Setup(m => m.GetMissionByRef(Procyon.Reference)).Returns(slowProcyon.Task);

        var cut = RenderMission(Procyon);
        cut.Render(p => p.Add(c => c.MissionRef, Pegasus.Reference));
        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Mission Pegasus"));

        // Procyon's lookup finishes after the user has already moved on to Pegasus.
        cut.InvokeAsync(() => slowProcyon.SetResult(Procyon));

        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("Mission Pegasus"));
        cut.Find(".mission-summary").TextContent.Should().Contain("Pegasus summary");
    }
}

/// <summary>Serves markdown by request path; anything else is a 404.</summary>
internal sealed class StubContentHandler : HttpMessageHandler
{
    private readonly System.Collections.Generic.Dictionary<string, string> files = new();

    public string this[string path] { set => files[path] = value; }

    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestCount++;
        var response = files.TryGetValue(request.RequestUri!.AbsolutePath, out var body)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "text/markdown") }
            : new HttpResponseMessage(HttpStatusCode.NotFound);
        return Task.FromResult(response);
    }
}
