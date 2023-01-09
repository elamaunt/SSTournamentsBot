using SSTournamentsBot.Api.DataDomain;
using System.Collections.Generic;

namespace SSTournamentsBot.Api.Services
{
    public interface IDataService
    {
        UserData FindUserByDiscordId(ulong discordId);
        bool StoreUsersSteamId(ulong discordId, ulong steamId);
        bool UpdateUser(UserData userData);
        void StoreTournament(TournamentData bundle);
        UserData[] LoadLeaders();
        void AddPenalty(ulong discordId, int penalty);
        bool DeleteUser(ulong discordId);
        UserData[] LoadAllsUsersWithScore();
        IEnumerable<UserData> EnumerateAllUsers();
    }
}
