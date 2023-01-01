using SSTournamentsBot.Api.DataDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IDataService
    {
        UserData FindUserByDiscordId(ulong discordId);
        void StoreUsersSteamId(ulong discordId, ulong steamId);
        UserData UpdateUser(UserData userData);
        void StoreTournament(TournamentData bundle);
        UserData[] LoadLeaders();
    }
}
