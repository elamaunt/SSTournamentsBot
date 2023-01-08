using SSTournamentsBot.Api.DataDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IDataService
    {
        UserData FindUserByDiscordId(ulong discordId);
        bool StoreUsersSteamId(ulong discordId, ulong steamId);
        UserData UpdateUser(UserData userData);
        void StoreTournament(TournamentData bundle);
        UserData[] LoadLeaders();
        void AddPenalty(ulong discordId, int penalty);
        bool DeleteUser(ulong discordId);
    }
}
