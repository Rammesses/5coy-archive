using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Models;

namespace Services;

public interface IVideoService : IItemsService<Video>
{
}

public class VideoService : IVideoService
{
    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    private Section<Video>[] videoData = null;

    public VideoService(HttpClient httpClient, ILogger<VideoService> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Video[]> GetAllItemsInTree(string sectionRef)
    {
        throw new NotImplementedException();
    }

    public Task<Video> GetItem(string sectionRef, string articleRef)
    {
        throw new NotImplementedException();
    }

    public Task<Video[]> GetItems(string sectionRef)
    {
        throw new NotImplementedException();
    }

    public async Task<Video[]> GetItemsByMissionRef(string missionRef)
    {
        await EnsureDataLoaded();

        var videos = Section<Video>.Flatten(videoData).SelectMany(s => s.Items).Where(a => a.MissionRef == missionRef).ToArray();
        return videos;
    }

    private async Task EnsureDataLoaded()
    {
        // load the section data from `data/sections.json`
        if (videoData == null)
        {
            var loadedData = await httpClient.GetFromJsonAsync<Section<Video>[]>("data/videos.json");
            lock(this)
            {
                videoData = loadedData;
                foreach (var section in videoData)
                {
                    section.Children.PopulateParentage(null);
                }
            }
        }    
    }
}