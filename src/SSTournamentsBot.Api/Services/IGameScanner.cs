using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.Services
{
    public interface IGameScanner
    {
        bool Active { get; set; }
        GameType GameTypeFilter { get; set; }
    }
}
