using HttpBuilder;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DowStatsApi : IStatsApi
    {
        readonly HttpService _service;
        readonly DowStatsApiOptions _options;
        public DowStatsApi(HttpService service, IOptions<DowStatsApiOptions> options)
        {
            _service = service;
            _options = options.Value;
        }

        public Task<Stats> LoadPlayerStats(ulong steamId)
        {
            return _service.Build("https://dowstats.ru/api/stats.php", UriKind.Absolute)
                .WithParameter("sids", steamId)
                .WithParameter("version", _options.Version)
                .WithHeader("key", _options.ApiKey)
                .Get()
                .Send()
                .ValidateSuccessStatusCode()
                .Json<DowStatsDTO[]>(JsonSerializer.CreateDefault())
                .Continue(x =>
                {
                    var first = x.FirstOrDefault();

                    if (first == null)
                        return new Stats(steamId, 0, 0, 0);
                    else
                        return new Stats(steamId, 0, first.gamesCount, first.mmr1v1);
                })
                .Task;
        }

        class DowStatsDTO
        {
            public string name { get; set; }
            public int gamesCount { get; set; }
            public bool isBanned { get; set; }
            public string avatarUrl { get; set; }
            public int winsCount { get; set; }
            public int winRate { get; set; }
            public int mmr { get; set; }
            public int calibrateGamesLeft { get; set; }
            public int race { get; set; }
            public int apm { get; set; }
            public int mmr1v1 { get; set; }
        }
    }
}
