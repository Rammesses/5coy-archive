using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Models;

using NUnit.Framework;

using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Services;

public class VideoServiceTests
{
    private static VideoGroup Inline(string reference, params Video[] videos)
        => new() { Reference = reference, Title = $"Group {reference}", Videos = videos };

    [Test]
    public async Task Loads_inline_videos_and_videos_from_a_group_Data_file()
    {
        var http = new StubHttpHandler().ServeJson("/data/videos/ic.json", new[] { Video("b"), Video("c") });
        var service = VideoService(new[]
        {
            Inline("ooc", Video("a")),
            new VideoGroup { Reference = "ic", Title = "IC", Data = "/data/videos/ic.json" },
        }, http);

        (await service.GetAllVideos()).Select(v => v.Reference).Should().Equal("a", "b", "c");
        (await service.GetVideo("c")).Group.Reference.Should().Be("ic");
    }

    [Test]
    public async Task Video_data_is_fetched_once_and_cached()
    {
        var http = new StubHttpHandler();
        var service = VideoService(new[] { Inline("g", Video("a")) }, http);

        await service.GetAllVideos();
        await service.GetVideo("a");
        await service.GetRelatedVideos("x");

        http.RequestsFor("data/videos.json").Should().Be(1);
    }

    [Test]
    public async Task GetItemsByMissionRef_and_GetRelatedVideos_match_their_own_fields()
    {
        var service = VideoService(new[] { Inline("g",
            Video("from-m", missionRef: "m"),
            Video("about-m", null, null, "m", "art"),
            Video("other")) });

        (await service.GetItemsByMissionRef("m")).Select(v => v.Reference).Should().Equal("from-m");
        (await service.GetRelatedVideos("m")).Select(v => v.Reference).Should().Equal("about-m");
        (await service.GetRelatedVideos("art")).Select(v => v.Reference).Should().Equal("about-m");
        (await service.GetItemsByMissionRef(null)).Should().BeEmpty();
    }

    [Test]
    public async Task GetSeries_returns_the_series_in_data_order()
    {
        var service = VideoService(new[] { Inline("g", Video("1", "s"), Video("x", "other"), Video("2", "s")) });

        var series = await service.GetSeries(await service.GetVideo("1"));

        series.Select(v => v.Reference).Should().Equal("1", "2");
    }

    [Test]
    public void Urls_are_built_from_the_YouTube_id()
    {
        var v = new Video { Reference = "r", YouTubeVideoId = "UF35M-yCeTc" };

        v.Url.Should().Be("/video/r");
        v.YouTubeUrl.Should().Be("https://www.youtube.com/watch?v=UF35M-yCeTc");
        v.EmbedUrl.Should().StartWith("https://www.youtube-nocookie.com/embed/UF35M-yCeTc");
        v.ThumbnailUrl.Should().Be("https://i.ytimg.com/vi/UF35M-yCeTc/hqdefault.jpg");
    }
}
