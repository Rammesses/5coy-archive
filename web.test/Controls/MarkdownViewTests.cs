using System.Collections.Generic;

using Bunit;

using FluentAssertions;

using NUnit.Framework;

using web.Controls;
using web.test.TestSupport;

namespace web.test.Controls;

public class MarkdownViewTests : ComponentTestBase
{
    private List<bool> loaded;

    [SetUp]
    public void ResetLoaded() => loaded = new List<bool>();

    private IRenderedComponent<MarkdownView> RenderView(string url)
        => Ctx.Render<MarkdownView>(p => p
            .Add(c => c.ContentUrl, url)
            .Add(c => c.ContentLoaded, (bool ok) => loaded.Add(ok)));

    [Test]
    public void Renders_markdown_as_html_and_reports_success()
    {
        Http["/content/a.md"] = "# Heading\n\nSome *text* and a [link](/mission/x).";

        var cut = RenderView("/content/a.md");

        cut.WaitForAssertion(() => loaded.Should().Equal(true));
        cut.Find("h1").TextContent.Should().Be("Heading");
        cut.Find("em").TextContent.Should().Be("text");
        cut.Find("a").GetAttribute("href").Should().Be("/mission/x");
    }

    [Test]
    public void Applies_bootstrap_classes_to_tables()
    {
        Http["/content/t.md"] = "| a | b |\n|---|---|\n| 1 | 2 |\n";

        var cut = RenderView("/content/t.md");

        cut.WaitForAssertion(() => cut.Find("table").ClassList.Should().Contain("table"));
    }

    [Test]
    public void A_missing_file_renders_nothing_and_reports_failure()
    {
        var cut = RenderView("/content/missing.md");

        cut.WaitForAssertion(() => loaded.Should().Equal(false));
        cut.Markup.Trim().Should().BeEmpty();
    }

    [Test]
    public void The_SPA_fallback_page_is_treated_as_missing()
    {
        // Static hosting serves index.html (200 text/html) for unknown paths.
        Http.Serve("/content/missing.md", "<!DOCTYPE html><html><body>app</body></html>", "text/html");

        var cut = RenderView("/content/missing.md");

        cut.WaitForAssertion(() => loaded.Should().Equal(false));
        cut.Markup.Should().NotContain("app");
    }

    [Test]
    public void No_url_renders_nothing_without_a_request()
    {
        var cut = RenderView(null);

        loaded.Should().Equal(false);
        cut.Markup.Trim().Should().BeEmpty();
        Http.RequestCount.Should().Be(0);
    }

    [Test]
    public void Changing_the_url_loads_the_new_content()
    {
        Http["/content/a.md"] = "Alpha";
        Http["/content/b.md"] = "Bravo";
        var cut = RenderView("/content/a.md");
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Alpha"));

        cut.Render(p => p.Add(c => c.ContentUrl, "/content/b.md"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Bravo");
            cut.Markup.Should().NotContain("Alpha");
        });
    }
}
