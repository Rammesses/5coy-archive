using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Models;

namespace Services;

public interface IMissionsService // : ISectionService<Mission>, IItemsService<Mission>
{
    Task<Mission[]> GetAllMissions();
    Task<Mission> GetMissionByRef(string missionRef);
}

public class MissionsService : IMissionsService
{
    private Mission[] missionData = null;

    private readonly HttpClient httpClient;
    private readonly ILogger logger;

    public MissionsService(HttpClient httpClient, ILogger<MissionsService> logger)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    private async Task EnsureDataLoaded()
    {
        // load the mission data from `data/sections.json`
        if (missionData == null)
        {
            var loadedData = await httpClient.GetFromJsonAsync<Mission[]>("data/missions.json");
            lock(this)
            {
                missionData = loadedData;
            }
        }
    }

    public async Task<Mission[]> GetAllMissions()
    {
        await EnsureDataLoaded();

        return missionData;
    }

    public async Task<Mission> GetMissionByRef(string missionRef) {
        await EnsureDataLoaded();

        return missionData.FirstOrDefault(m => m.Reference == missionRef);
    }
}