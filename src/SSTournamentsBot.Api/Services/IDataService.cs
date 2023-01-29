using SSTournamentsBot.Api.DataDomain;
using System.Collections.Generic;

namespace SSTournamentsBot.Api.Services
{
    public interface IDataService
    {
        UserData FindUserByDiscordId(ulong discordId);
        bool StoreUsersSteamId(ulong discordId, ulong steamId);
        bool UpdateUser(UserData userData);
        void StoreTournamentAndIncrementTournamentId(TournamentData bundle);
        UserData[] LoadLeadersVanilla();
        void AddPenalty(ulong discordId, int penalty);
        bool DeleteUser(ulong discordId);
        UserData[] LoadAllsUsersWithScore();
        IEnumerable<UserData> EnumerateAllUsers();
        (int SeasonId, int TournamentId) GetCurrentTournamentIds();
        void IncrementTournamentId();

        UserInActivityModel FindUserActivity(string contextName, ulong discordId, ulong steamId);
        bool UpdateUserInActivity(string contextName, UserInActivityModel data);
        UserInActivityModel[] LoadLeaders(string contextName);
    }
}
