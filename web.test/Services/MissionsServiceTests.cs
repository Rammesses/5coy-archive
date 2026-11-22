using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using Services;

using web.test.TestSupport;

using static web.test.TestSupport.TestData;

namespace web.test.Services;

public class MissionsServiceTests
{
    [Test]
    public async Task GetAllMissions_returns_every_mission_from_missions_json()
    {
        var service = MissionsService(Mission("a"), Mission("b"));

        var all = await service.GetAllMissions();

        all.Select(m => m.Reference).Should().Equal("a", "b");
    }

    [Test]
    public async Task Mission_data_is_fetched_once_and_cached()
    {
        var http = new StubHttpHandler().ServeJson("data/missions.json", new[] { Mission("a") });
        var service = new MissionsService(http.CreateClient(), NullLogger<MissionsService>.Instance);

        await service.GetAllMissions();
        await service.GetMissionByRef("a");
        await service.GetRelatedMissions("a", null);

        http.RequestsFor("data/missions.json").Should().Be(1);
    }

    [Test]
    public async Task GetMissionByRef_finds_the_mission()
    {
        var service = MissionsService(Mission("a"), Mission("b"));

        (await service.GetMissionByRef("b")).Reference.Should().Be("b");
    }

    [Test]
    public async Task GetMissionByRef_returns_null_for_an_unknown_reference()
    {
        var service = MissionsService(Mission("a"));

        (await service.GetMissionByRef("nope")).Should().BeNull();
    }

    [Test]
    public async Task GetRelatedMissions_includes_missions_this_one_links_to()
    {
        var service = MissionsService(Mission("procyon", "pegasus"), Mission("pegasus"), Mission("other"));

        var related = await service.GetRelatedMissions("procyon", new[] { "pegasus" });

        related.Select(m => m.Reference).Should().Equal("pegasus");
    }

    [Test]
    public async Task GetRelatedMissions_includes_missions_that_link_back_to_this_one()
    {
        // Pegasus names Procyon, Procyon names nothing: Procyon's page should still show Pegasus.
        var service = MissionsService(Mission("procyon"), Mission("pegasus", "procyon"));

        var related = await service.GetRelatedMissions("procyon", Array.Empty<string>());

        related.Select(m => m.Reference).Should().Equal("pegasus");
    }

    [Test]
    public async Task GetRelatedMissions_works_for_an_article_reference()
    {
        // Article pages call this with the article's Reference to find missions that name it.
        var service = MissionsService(Mission("pegasus", "newsletter-12"), Mission("other"));

        var related = await service.GetRelatedMissions("newsletter-12", new[] { "other" });

        related.Select(m => m.Reference).Should().BeEquivalentTo("pegasus", "other");
    }

    [Test]
    public async Task GetRelatedMissions_lists_a_mission_linked_both_ways_only_once()
    {
        var service = MissionsService(Mission("procyon", "pegasus"), Mission("pegasus", "procyon"));

        var related = await service.GetRelatedMissions("procyon", new[] { "pegasus" });

        related.Select(m => m.Reference).Should().Equal("pegasus");
    }

    [Test]
    public async Task GetRelatedMissions_never_includes_the_mission_itself()
    {
        var service = MissionsService(Mission("procyon", "procyon"), Mission("pegasus"));

        var related = await service.GetRelatedMissions("procyon", new[] { "procyon", "pegasus" });

        related.Select(m => m.Reference).Should().Equal("pegasus");
    }

    [Test]
    public async Task GetRelatedMissions_skips_references_that_are_not_missions()
    {
        // Related lists mix article and mission references; only the missions come back.
        var service = MissionsService(Mission("pegasus"));

        var related = await service.GetRelatedMissions("procyon", new[] { "an-article", "pegasus", "missing" });

        related.Select(m => m.Reference).Should().Equal("pegasus");
    }

    [Test]
    public async Task GetRelatedMissions_handles_a_null_related_list()
    {
        var service = MissionsService(Mission("procyon"), Mission("pegasus"));

        var related = await service.GetRelatedMissions("procyon", null);

        related.Should().BeEmpty();
    }

    [Test]
    public async Task GetRelatedMissions_handles_missions_with_a_null_related_list()
    {
        var withoutRelated = Mission("pegasus");
        withoutRelated.Related = null;
        var service = MissionsService(withoutRelated, Mission("procyon"));

        var related = await service.GetRelatedMissions("procyon", null);

        related.Should().BeEmpty();
    }
}
