namespace Models;

/// <summary>
/// One change listed on the Change History page (<c>wwwroot/change-history.md</c>): a level-2
/// heading of the form <c>## {date} - {title} {#{id}}</c> followed by a summary paragraph.
/// </summary>
public class ChangeHistoryEntry
{
    public const string PageUrl = "/change-history";
    public const string ContentUrl = "/change-history.md";

    /// <summary>The heading's explicit id, e.g. <c>2026-11-25-videos</c>.</summary>
    public string Id { get; set; }

    /// <summary>The date as written in the heading, e.g. "25 November 2026" or "October 2025".</summary>
    public string Date { get; set; }

    public string Title { get; set; }

    /// <summary>The first paragraph after the heading, rendered as HTML (without the &lt;p&gt;).</summary>
    public string SummaryHtml { get; set; }

    /// <summary>Marked <c>.whats-new</c>: an important change, shown on the home page.</summary>
    public bool IsHighlight { get; set; }

    public string Url => string.IsNullOrEmpty(Id) ? PageUrl : $"{PageUrl}#{Id}";
}
