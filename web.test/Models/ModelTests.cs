using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Models;

using NUnit.Framework;

using Services;

using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Models;

public class SectionTests
{
    [Test]
    public void Url_defaults_to_the_section_page()
        => new Section<Article> { Reference = "ic" }.Url.Should().Be("/section/ic");

    [Test]
    public void Url_uses_NavigateUrl_when_set()
        => new Section<Article> { Reference = "missions", NavigateUrl = "/missions" }.Url.Should().Be("/missions");

    [Test]
    public void BreadcrumbTitle_prefers_ShortTitle()
        => new Section<Article> { Title = "In-Character", ShortTitle = "IC" }.BreadcrumbTitle.Should().Be("IC");

    [Test]
    public void BreadcrumbTitle_falls_back_to_Title()
        => new Section<Article> { Title = "In-Character" }.BreadcrumbTitle.Should().Be("In-Character");

    [Test]
    public void Flatten_includes_every_level()
    {
        var tree = new[] { Section("a", Section("b", Section("c"))), Section("d") };

        Section<Article>.Flatten(tree).Select(s => s.Reference).Should().BeEquivalentTo("a", "b", "c", "d");
    }
}

public class HeirarchicalItemTests
{
    [Test]
    public void PopulateParentage_sets_each_childs_parent_at_every_depth()
    {
        var grandchild = Article("grandchild");
        var child = Article("child", children: grandchild);
        var root = Article("root", children: child);

        new IHeirarchicalItem<Article>[] { root }.PopulateParentage(null);

        root.Parent.Should().BeNull();
        child.Parent.Should().BeSameAs(root);
        grandchild.Parent.Should().BeSameAs(child);
    }

    [Test]
    public void PopulateParentage_keeps_an_existing_parent()
    {
        var original = Article("original");
        var child = Article("child");
        child.Parent = original;
        var root = Article("root", children: child);

        new IHeirarchicalItem<Article>[] { root }.PopulateParentage(null);

        child.Parent.Should().BeSameAs(original);
    }
}

public class VideoServiceTests
{
    [Test]
    public async Task GetItemsByMissionRef_finds_videos_inline_and_in_group_data_files()
    {
        var groups = new[]
        {
            new VideoGroup
            {
                Reference = "videos",
                Videos = new[] { new Video { Reference = "v1", MissionRef = "pegasus" }, new Video { Reference = "v2", MissionRef = "other" } },
            },
            new VideoGroup { Reference = "more", Data = "data/videos/more.json" },
        };
        var http = new StubHttpHandler()
            .ServeJson("data/videos/more.json", new[] { new Video { Reference = "v3", MissionRef = "pegasus" } });
        var service = VideoService(groups, http);

        var found = await service.GetItemsByMissionRef("pegasus");

        found.Select(v => v.Reference).Should().BeEquivalentTo("v1", "v3");
    }
}
