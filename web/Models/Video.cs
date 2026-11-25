using System;
using System.Text.Json.Serialization;

namespace Models;

/// <summary>
/// A group of videos in <c>data/videos.json</c> (e.g. in-character, out-of-character). Like an
/// article group, it either inlines its <see cref="Videos"/> or points at a <see cref="Data"/> file
/// holding a <c>Video[]</c>.
/// </summary>
public class VideoGroup
{
    public string Reference { get; set; }
    public string Title { get; set; }
    public string Data { get; set; }
    public Video[] Videos { get; set; } = Array.Empty<Video>();
}

/// <summary>A YouTube video shown on the /videos listing and its own /video/{Reference} page.</summary>
public class Video
{
    public string Reference { get; set; }
    public string Title { get; set; }

    /// <summary>Videos are listed together under their series (e.g. "Krystal Spyers Reports - Altair IV").</summary>
    public string Series { get; set; }

    public string YouTubeVideoId { get; set; }

    /// <summary>The title as uploaded to YouTube, which may spell names differently from the archive.</summary>
    public string YouTubeTitle { get; set; }

    /// <summary>The YouTube channel that uploaded it.</summary>
    public string Channel { get; set; }

    /// <summary>Real-world upload date (yyyy-mm-dd), out of character.</summary>
    public string Uploaded { get; set; }

    /// <summary>Running time as YouTube shows it, e.g. "7:23".</summary>
    public string Duration { get; set; }

    public string Description { get; set; }

    /// <summary>Markdown for the video page: notes, cross-references and the transcript.</summary>
    public string ContentUrl { get; set; }

    /// <summary>The mission the footage is from (lists the video on the mission page).</summary>
    public string MissionRef { get; set; }

    /// <summary>
    /// Article and mission References the video relates to without being from that mission.
    /// The video is listed under "Related" on those pages too.
    /// </summary>
    public string[] Related { get; set; } = Array.Empty<string>();

    [JsonIgnore]
    public VideoGroup Group { get; set; }

    public string Url => $"/video/{Reference}";

    private string Id => Uri.EscapeDataString(YouTubeVideoId ?? string.Empty);

    public string YouTubeUrl => $"https://www.youtube.com/watch?v={Id}";

    /// <summary>The privacy-enhanced embed: no YouTube cookies until the visitor plays it.</summary>
    public string EmbedUrl => $"https://www.youtube-nocookie.com/embed/{Id}?rel=0";

    public string ThumbnailUrl => $"https://i.ytimg.com/vi/{Id}/hqdefault.jpg";
}
