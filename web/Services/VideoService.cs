using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Models;

namespace Services;

public interface IVideoService
{
    /// <summary>The groups in <c>data/videos.json</c>, with each group's videos loaded.</summary>
    Task<VideoGroup[]> GetGroups();

    /// <summary>Every video, in data order.</summary>
    Task<Video[]> GetAllVideos();

    Task<Video> GetVideo(string videoRef);

    /// <summary>Videos whose MissionRef is the mission.</summary>
    Task<Video[]> GetItemsByMissionRef(string missionRef);

    /// <summary>Videos whose Related list names <paramref name="reference"/> (an article or mission Reference).</summary>
    Task<Video[]> GetRelatedVideos(string reference);

    /// <summary>The other videos in the same series, in order, including <paramref name="video"/> itself.</summary>
    Task<Video[]> GetSeries(Video video);
}

public class VideoService : IVideoService
{
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    private VideoGroup[] groupData = null;

    public VideoService(HttpClient httpClient, ILogger<VideoService> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VideoGroup[]> GetGroups()
    {
        await EnsureDataLoaded();
        return groupData;
    }

    public async Task<Video[]> GetAllVideos()
    {
        await EnsureDataLoaded();
        return groupData.SelectMany(g => g.Videos).ToArray();
    }

    public async Task<Video> GetVideo(string videoRef)
        => (await GetAllVideos()).FirstOrDefault(v => v.Reference == videoRef);

    public async Task<Video[]> GetItemsByMissionRef(string missionRef)
    {
        if (string.IsNullOrEmpty(missionRef))
            return Array.Empty<Video>();

        return (await GetAllVideos()).Where(v => v.MissionRef == missionRef).ToArray();
    }

    public async Task<Video[]> GetRelatedVideos(string reference)
    {
        if (string.IsNullOrEmpty(reference))
            return Array.Empty<Video>();

        return (await GetAllVideos()).Where(v => v.Related?.Contains(reference) == true).ToArray();
    }

    public async Task<Video[]> GetSeries(Video video)
    {
        if (video == null)
            return Array.Empty<Video>();
        if (string.IsNullOrEmpty(video.Series))
            return new[] { video };

        return (await GetAllVideos()).Where(v => v.Series == video.Series).ToArray();
    }

    private async Task EnsureDataLoaded()
    {
        if (groupData != null)
            return;

        var loaded = await httpClient.GetFromJsonAsync<VideoGroup[]>("data/videos.json") ?? Array.Empty<VideoGroup>();
        foreach (var group in loaded)
        {
            var videos = new List<Video>(group.Videos ?? Array.Empty<Video>());
            if (!string.IsNullOrEmpty(group.Data))
            {
                var more = await httpClient.GetFromJsonAsync<Video[]>(group.Data);
                if (more != null)
                    videos.AddRange(more);
            }

            foreach (var video in videos)
                video.Group = group;
            group.Videos = videos.ToArray();
        }

        lock (this)
        {
            groupData ??= loaded;
        }
    }
}
