using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IStatsApi
    {
        Task<Stats> LoadPlayerStats(ulong steamId);
    }
}