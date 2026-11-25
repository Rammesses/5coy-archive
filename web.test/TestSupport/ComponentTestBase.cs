using System;
using System.Collections.Generic;
using System.Net.Http;

using Bunit;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Models;

using Moq;

using NUnit.Framework;

using Services;

namespace web.test.TestSupport;

/// <summary>
/// bUnit context with everything the archive's components inject: mocked data services that
/// return empty results by default, a stub HTTP handler for markdown, and configuration.
/// </summary>
public abstract class ComponentTestBase
{
    protected BunitContext Ctx { get; private set; }
    protected Mock<IMissionsService> Missions { get; private set; }
    protected Mock<IArticlesService> Articles { get; private set; }
    protected Mock<IVideoService> Videos { get; private set; }
    protected Mock<IChangeHistoryService> ChangeHistory { get; private set; }
    internal StubHttpHandler Http { get; private set; }

    /// <summary>Configuration values (e.g. "Media:BaseUrl"); set before the first render.</summary>
    protected Dictionary<string, string> Config { get; private set; }

    [SetUp]
    public void SetUpContext()
    {
        Config = new Dictionary<string, string>();
        Ctx = new BunitContext();
        Ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        Missions = new Mock<IMissionsService>();
        Missions.Setup(m => m.GetAllMissions()).ReturnsAsync(Array.Empty<Mission>());
        Missions.Setup(m => m.GetRelatedMissions(It.IsAny<string>(), It.IsAny<string[]>())).ReturnsAsync(Array.Empty<Mission>());

        Articles = new Mock<IArticlesService>();
        Articles.Setup(a => a.GetItemsByMissionRef(It.IsAny<string>())).ReturnsAsync(Array.Empty<Article>());
        Articles.Setup(a => a.GetRelatedItems(It.IsAny<Article>())).ReturnsAsync(Array.Empty<Article>());
        Articles.Setup(a => a.GetRelatedItemsForMission(It.IsAny<Mission>())).ReturnsAsync(Array.Empty<Article>());
        Articles.Setup(a => a.GetSectionPathForItem(It.IsAny<Article>())).ReturnsAsync(Array.Empty<Section<Article>>());
        Articles.Setup(a => a.GetItems(It.IsAny<string>())).ReturnsAsync(Array.Empty<Article>());
        Articles.Setup(a => a.GetAllItemsInTree(It.IsAny<string>())).ReturnsAsync(Array.Empty<Article>());

        Videos = new Mock<IVideoService>();
        Videos.Setup(v => v.GetItemsByMissionRef(It.IsAny<string>())).ReturnsAsync(Array.Empty<Video>());
        Videos.Setup(v => v.GetRelatedVideos(It.IsAny<string>())).ReturnsAsync(Array.Empty<Video>());
        Videos.Setup(v => v.GetAllVideos()).ReturnsAsync(Array.Empty<Video>());
        Videos.Setup(v => v.GetSeries(It.IsAny<Video>())).ReturnsAsync(Array.Empty<Video>());

        ChangeHistory = new Mock<IChangeHistoryService>();
        ChangeHistory.Setup(c => c.GetEntries()).ReturnsAsync(Array.Empty<ChangeHistoryEntry>());
        ChangeHistory.Setup(c => c.GetHighlights(It.IsAny<int>())).ReturnsAsync(Array.Empty<ChangeHistoryEntry>());

        Http = new StubHttpHandler();

        Ctx.Services.AddSingleton(Missions.Object);
        Ctx.Services.AddSingleton(Articles.Object);
        Ctx.Services.AddSingleton(Videos.Object);
        Ctx.Services.AddSingleton(ChangeHistory.Object);
        Ctx.Services.AddSingleton(Mock.Of<IMarkdownService>());
        Ctx.Services.AddSingleton(Http.CreateClient());
        Ctx.Services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().AddInMemoryCollection(Config).Build());
    }

    [TearDown]
    public void TearDownContext() => Ctx.Dispose();
}
