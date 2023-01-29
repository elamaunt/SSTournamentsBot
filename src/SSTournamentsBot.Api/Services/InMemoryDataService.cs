using SSTournamentsBot.Api.DataDomain;
using System.Collections.Generic;
using System.Linq;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.Services
{
    public class InMemoryDataService : IDataService
    {
        readonly Dictionary<ulong, UserInActivityModel> _userInActivities = new Dictionary<ulong, UserInActivityModel>()
        {
        };

        readonly Dictionary<ulong, UserData> _users = new Dictionary<ulong, UserData>()
        {
            { 272710324484833281, new UserData() {
                SteamId = 76561198001658409,
                DiscordId = 272710324484833281,
                Race = RaceOrRandom.NewRace(Race.Orks)
            } }
        };

        public void AddPenalty(ulong discordId, int penalty)
        {
            if (_users.TryGetValue(discordId, out var data))
            {
                data.Penalties += penalty;
                _users[discordId] = data;
            }
        }

        public bool DeleteUser(ulong id)
        {
            return false;
        }

        public IEnumerable<UserData> EnumerateAllUsers()
        {
            return _users.Values;
        }

        public UserData FindUserByDiscordId(ulong discordId)
        {
            _users.TryGetValue(discordId, out var data);
            return data;
        }

        public (int SeasonId, int TournamentId) GetCurrentTournamentIds()
        {
            return (0, 0);
        }

        public void IncrementTournamentId()
        {
            // Nothing
        }

        public UserData[] LoadAllsUsersWithScore()
        {
            return new UserData[0];
        }

        public UserData[] LoadLeadersVanilla()
        {
            return _users
                .Where(x => x.Value.Score != 0)
                .OrderByDescending(x => x.Value.Score)
                .Take(20)
                .Select(x => x.Value)
                .ToArray();
        }

        public void StoreTournamentAndIncrementTournamentId(TournamentData data)
        {
            // Nothing
        }

        public bool StoreUsersSteamId(ulong discordId, ulong steamId)
        {
            _users.TryGetValue(discordId, out var data);

            data = data ?? new UserData();

            data.DiscordId = discordId;
            data.SteamId = steamId;

            _users[discordId] = data;

            return true;
        }

        public bool UpdateUser(UserData userData)
        {
            _users[userData.DiscordId] = userData;
            return true;
        }

        public UserInActivityModel FindUserActivity(string contextName, ulong discordId, ulong steamId)
        {
            _userInActivities.TryGetValue(discordId, out var data);

            data = data ?? new UserInActivityModel();

            data.DiscordId = discordId;
            data.SteamId = steamId;

            _userInActivities[discordId] = data;

            return data;
        }

        public bool UpdateUserInActivity(string contextName, UserInActivityModel data)
        {
            _userInActivities[data.DiscordId] = data;
            return true;
        }

        public UserInActivityModel[] LoadLeaders(string contextName)
        {
            return _userInActivities
                .Where(x => x.Value.Score != 0)
                .OrderByDescending(x => x.Value.Score)
                .Take(20)
                .Select(x => x.Value)
                .ToArray();
        }
    }
}
