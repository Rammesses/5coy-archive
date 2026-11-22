using System;
using System.Collections.Generic;

namespace Models;

public class Section<T> : IHeirarchicalItem<Section<T>>
{
    public const string AllItemsRef = @"all";
    
    public string Reference { get; set; }
    public string Title { get; set;}

    /// <summary>Optional abbreviation used in breadcrumbs (e.g. "IC"); falls back to Title.</summary>
    public string ShortTitle { get; set; }

    public string BreadcrumbTitle => string.IsNullOrEmpty(ShortTitle) ? Title : ShortTitle;
    public string ContentUrl { get; set; }
    public string NavigateUrl { get; set; }

    public string Glyph { get; set; }

    public string Url => NavigateUrl ?? $"/section/{Reference}";

    public T[] Items { get; set; } = Array.Empty<T>();

    public Section<T>[] Children { get; set; } = Array.Empty<Section<T>>();

    public Section<T> Parent { get; set; }

    public static IEnumerable<Section<T>> Flatten (IEnumerable<Section<T>> list)
        => Extensions.EnumerableExtensions.Flatten<Section<T>>(list, a => a.Children);

}
