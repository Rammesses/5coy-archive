using System;
using System.Linq;

using Bunit;

using FluentAssertions;

using Models;

using Moq;

using NUnit.Framework;

using web.Pages;
using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Pages;

public class VideosPageTests : ComponentTestBase
{
    [Test]
    public void Lists_videos_under_their_series_with_thumbnails_and_mission_links()
    {
        var mission = Mission("2495-05_m");
        Missions.Setup(m => m.GetAllMissions()).ReturnsAsync(new[] { mission });
        Videos.Setup(v => v.GetAllVideos()).ReturnsAsync(new[]
        {
            Video("a", "Series One", "2495-05_m"),
            Video("b", "Series Two"),
            Video("c", "Series One"),
        });

        var cut = Ctx.Render<web.Pages.Videos>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("section.video-series h3").Select(h => h.TextContent).Should().Equal("Series One", "Series Two");
            cut.FindAll("section.video-series")[0].QuerySelectorAll("a.video-title").Select(a => a.GetAttribute("href"))
                .Should().Equal("/video/a", "/video/c");
            cut.Find("a.video-thumbnail img").GetAttribute("src").Should().Contain("yt-a");
            cut.Find("div.video-mission a").GetAttribute("href").Should().Be("/mission/2495-05_m");
        });
    }
}

public class VideoDetailTests : ComponentTestBase
{
    private IRenderedComponent<VideoDetail> RenderVideo(string reference)
        => Ctx.Render<VideoDetail>(p => p.Add(c => c.VideoRef, reference));

    [Test]
    public void Embeds_the_video_and_shows_its_notes_and_mission()
    {
        var mission = Mission("m");
        var video = Video("v", "s", "m");
        video.ContentUrl = "/content/videos/v.md";
        Videos.Setup(x => x.GetVideo("v")).ReturnsAsync(video);
        Missions.Setup(x => x.GetMissionByRef("m")).ReturnsAsync(mission);
        Http["/content/videos/v.md"] = "Transcript goes here.";

        var cut = RenderVideo("v");

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("Video v");
            cut.Find("div.video-player iframe").GetAttribute("src").Should().StartWith("https://www.youtube-nocookie.com/embed/yt-v");
            cut.Find(".page-mission a").TextContent.Should().Be(mission.LinkText);
            cut.Markup.Should().Contain("Transcript goes here.");
        });
    }

    [Test]
    public void Links_to_the_previous_and_next_videos_in_the_series()
    {
        var series = new[] { Video("1", "s"), Video("2", "s"), Video("3", "s") };
        Videos.Setup(x => x.GetVideo("2")).ReturnsAsync(series[1]);
        Videos.Setup(x => x.GetSeries(series[1])).ReturnsAsync(series);

        var cut = RenderVideo("2");

        cut.WaitForAssertion(() =>
        {
            cut.Find("a.video-previous").GetAttribute("href").Should().Be("/video/1");
            cut.Find("a.video-next").GetAttribute("href").Should().Be("/video/3");
        });
    }

    [Test]
    public void Lists_related_articles()
    {
        var video = Video("v", null, null, "art");
        var article = Article("art");
        Videos.Setup(x => x.GetVideo("v")).ReturnsAsync(video);
        Articles.Setup(a => a.GetItem(Section<Article>.AllItemsRef, "art")).ReturnsAsync(article);

        var cut = RenderVideo("v");

        cut.WaitForAssertion(() => cut.Find(".related-articles").TextContent.Should().Contain("Title of art"));
    }

    [Test]
    public void An_unknown_video_says_so()
    {
        var cut = RenderVideo("nope");

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("no video called 'nope'"));
    }
}

public class RelatedVideosOnOtherPagesTests : ComponentTestBase
{
    [Test]
    public void A_mission_page_lists_its_videos_and_videos_that_relate_to_it()
    {
        var mission = Mission("m");
        Missions.Setup(x => x.GetMissionByRef("m")).ReturnsAsync(mission);
        Videos.Setup(x => x.GetItemsByMissionRef("m")).ReturnsAsync(new[] { Video("from") });
        Videos.Setup(x => x.GetRelatedVideos("m")).ReturnsAsync(new[] { Video("about") });

        var cut = Ctx.Render<MissionDetail>(p => p.Add(c => c.MissionRef, "m"));

        cut.WaitForAssertion(() =>
            cut.FindAll("li.related-video a").Select(a => a.GetAttribute("href"))
               .Should().Contain(new[] { "/video/from", "/video/about" }));
    }
}
