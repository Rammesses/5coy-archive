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

public class SectionDetailTests : ComponentTestBase
{
    private Section<Article> ic;
    private Section<Article> ilof;

    [SetUp]
    public void SetUp()
    {
        ilof = new Section<Article> { Reference = "ilof", Title = "In The Line Of Fire", ContentUrl = "/content/ilof.md" };
        ic = new Section<Article>
        {
            Reference = "ic",
            Title = "In-Character",
            ContentUrl = "/content/ic.md",
            Children = new[]
            {
                new Section<Article> { Reference = "reports", Title = "Mission Reports" },
                new Section<Article> { Reference = "letters", Title = "Mexal's Letters" },
                new Section<Article> { Reference = "missions", Title = "Missions", NavigateUrl = "/missions" },
            },
        };
        Articles.Setup(a => a.GetSection("ic")).ReturnsAsync(ic);
        Articles.Setup(a => a.GetSection("ilof")).ReturnsAsync(ilof);
        Http["/content/ic.md"] = "In-character intro.";
        Http["/content/ilof.md"] = "ILOF intro.";
    }

    private IRenderedComponent<SectionDetail> RenderSection(string reference)
        => Ctx.Render<SectionDetail>(p => p.Add(c => c.SectionRef, reference));

    [Test]
    public void Renders_the_title_and_intro()
    {
        var cut = RenderSection("ic");

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("In-Character");
            cut.Markup.Should().Contain("In-character intro.");
        });
    }

    [Test]
    public void A_sub_section_links_back_to_its_parent_section()
    {
        ilof.Parent = ic;

        var cut = RenderSection("ilof");

        cut.WaitForAssertion(() =>
        {
            var back = cut.Find("a.parent-section");
            back.GetAttribute("href").Should().Be("/section/ic");
            back.TextContent.Should().Contain("In-Character");
        });
    }

    [Test]
    public void A_top_level_section_has_no_back_link()
    {
        var cut = RenderSection("ic");

        cut.WaitForAssertion(() => cut.Find("h1").TextContent.Should().Contain("In-Character"));
        cut.FindAll("a.parent-section").Should().BeEmpty();
    }

    [Test]
    public void Child_sections_are_linked_in_title_order()
    {
        var cut = RenderSection("ic");

        cut.WaitForAssertion(() =>
            cut.FindAll("a.section-link").Select(a => a.GetAttribute("href")).Should().Equal(
                "/section/letters", "/section/reports", "/missions"));
    }

    [Test]
    public void Articles_link_to_their_page_and_untitled_ones_are_plain_text()
    {
        Articles.Setup(a => a.GetItems("ilof")).ReturnsAsync(new[] { Article("ilof-1"), new Article { Title = "No reference" } });

        var cut = RenderSection("ilof");

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".lead a").Select(a => a.GetAttribute("href")).Should().Equal("/article/ilof/ilof-1");
            cut.Markup.Should().Contain("No reference");
        });
    }

    [Test]
    public void Articles_with_a_pdf_show_a_download_link()
    {
        var withPdf = Article("ilof-1");
        withPdf.PdfUrl = "/media/ilof-1.pdf";
        Articles.Setup(a => a.GetItems("ilof")).ReturnsAsync(new[] { withPdf });

        var cut = RenderSection("ilof");

        cut.WaitForAssertion(() =>
            cut.Find("a[title='Download as PDF']").GetAttribute("href").Should().Be("/media/ilof-1.pdf"));
    }

    [Test]
    public void Navigating_to_another_section_reloads_the_page()
    {
        var cut = RenderSection("ic");
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("In-character intro."));

        cut.Render(p => p.Add(c => c.SectionRef, "ilof"));

        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.Should().Contain("In The Line Of Fire");
            cut.Markup.Should().Contain("ILOF intro.");
            cut.FindAll("a.section-link").Should().BeEmpty();
        });
    }
}

public class MissionsPageTests : ComponentTestBase
{
    private static Mission M(string reference, int year, int month) => new()
    {
        Reference = reference,
        Name = $"Mission {reference}",
        StartDate = new DateTime(year, month, 1),
        EndDate = new DateTime(year, month, 3),
    };

    [SetUp]
    public void SetUp()
    {
        Missions.Setup(m => m.GetAllMissions()).ReturnsAsync(new[] { M("pegasus", 1994, 6), M("procyon", 1992, 10), M("ogre", 1994, 4) });
        // Give every mission a report so its toggle button is enabled.
        Articles.Setup(a => a.GetItemsByMissionRef(It.IsAny<string>())).ReturnsAsync(new[] { Article("report") });
    }

    [Test]
    public void Missions_are_listed_in_date_order()
    {
        var cut = Ctx.Render<Missions>();

        cut.WaitForAssertion(() =>
            cut.FindAll("tbody td a[href^='/mission/']").Select(a => a.TextContent).Should().Equal(
                "Mission procyon", "Mission ogre", "Mission pegasus"));
    }

    [Test]
    public void Expanding_a_mission_collapses_the_others_and_clicking_again_closes_it()
    {
        var cut = Ctx.Render<Missions>();
        cut.WaitForAssertion(() => cut.FindAll("tbody button").Count.Should().Be(3));

        string[] Expanded() => cut.FindAll("tbody button").Select(b => b.GetAttribute("aria-expanded")).ToArray();

        cut.FindAll("tbody button")[0].Click();
        Expanded().Should().Equal("true", "false", "false");

        cut.FindAll("tbody button")[2].Click();
        Expanded().Should().Equal("false", "false", "true");

        cut.FindAll("tbody button")[2].Click();
        Expanded().Should().Equal("false", "false", "false");
    }
}
