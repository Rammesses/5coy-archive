using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using Microsoft.Extensions.Logging;

using Models;

namespace Services;

public interface IChangeHistoryService
{
    /// <summary>Every entry on the Change History page, newest first (document order).</summary>
    Task<ChangeHistoryEntry[]> GetEntries();

    /// <summary>The newest <paramref name="count"/> entries marked <c>.whats-new</c>.</summary>
    Task<ChangeHistoryEntry[]> GetHighlights(int count = 3);
}

/// <summary>
/// Reads <c>change-history.md</c> so the home page's "What's New" is driven by the same
/// markdown as the Change History page: no second copy of the list to keep in step.
/// </summary>
public class ChangeHistoryService : IChangeHistoryService
{
    public const string HighlightClass = "whats-new";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private readonly HttpClient httpClient;
    private readonly ILogger<ChangeHistoryService> logger;
    private ChangeHistoryEntry[] entries;

    public ChangeHistoryService(HttpClient httpClient, ILogger<ChangeHistoryService> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChangeHistoryEntry[]> GetEntries()
    {
        if (entries != null)
            return entries;

        try
        {
            var response = await httpClient.GetAsync(ChangeHistoryEntry.ContentUrl);
            // A missing file can come back as the SPA's index.html via the navigation fallback.
            if (!response.IsSuccessStatusCode ||
                response.Content.Headers.ContentType?.MediaType == "text/html")
            {
                logger.LogWarning("Change history not found at {Url}", ChangeHistoryEntry.ContentUrl);
                return entries = Array.Empty<ChangeHistoryEntry>();
            }

            entries = Parse(await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Couldn't load the change history");
            entries = Array.Empty<ChangeHistoryEntry>();
        }

        return entries;
    }

    public async Task<ChangeHistoryEntry[]> GetHighlights(int count = 3)
        => (await GetEntries()).Where(e => e.IsHighlight).Take(count).ToArray();

    /// <summary>Parse the change history markdown into its entries.</summary>
    public static ChangeHistoryEntry[] Parse(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
        var result = new List<ChangeHistoryEntry>();
        var blocks = document.ToList();

        for (var i = 0; i < blocks.Count; i++)
        {
            if (blocks[i] is not HeadingBlock { Level: 2 } heading)
                continue;

            var attributes = heading.TryGetAttributes();
            var (date, title) = SplitHeading(InlineText(heading.Inline));

            var summary = blocks.Skip(i + 1)
                .TakeWhile(b => b is not HeadingBlock)
                .OfType<ParagraphBlock>()
                .FirstOrDefault();

            result.Add(new ChangeHistoryEntry
            {
                Id = attributes?.Id,
                Date = date,
                Title = title,
                SummaryHtml = summary == null ? string.Empty : RenderParagraph(markdown, summary),
                IsHighlight = attributes?.Classes?.Contains(HighlightClass) == true,
            });
        }

        return result.ToArray();
    }

    /// <summary>"25 November 2026 - Videos" → ("25 November 2026", "Videos"); no " - " → (null, text).</summary>
    private static (string Date, string Title) SplitHeading(string text)
    {
        var at = text.IndexOf(" - ", StringComparison.Ordinal);
        return at < 0 ? (null, text.Trim()) : (text[..at].Trim(), text[(at + 3)..].Trim());
    }

    private static string RenderParagraph(string markdown, ParagraphBlock paragraph)
    {
        var source = markdown.Substring(paragraph.Span.Start, paragraph.Span.Length);
        var html = Markdown.ToHtml(source, Pipeline).Trim();
        if (html.StartsWith("<p>") && html.EndsWith("</p>"))
            html = html[3..^4];
        return html;
    }

    private static string InlineText(ContainerInline inline)
    {
        var text = new StringBuilder();
        if (inline != null)
            Append(inline, text);
        return text.ToString();

        static void Append(Inline node, StringBuilder sb)
        {
            switch (node)
            {
                case LiteralInline literal: sb.Append(literal.Content.ToString()); break;
                case CodeInline code: sb.Append(code.Content); break;
                case LineBreakInline: sb.Append(' '); break;
                case ContainerInline container:
                    foreach (var child in container) Append(child, sb);
                    break;
            }
        }
    }
}
