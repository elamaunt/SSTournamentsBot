using LiteDB;
using SSTournamentsBot.Api.DataDomain;
using System;
using System.Collections.Generic;
using System.Linq;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class LiteDbDataService : IDataService
    {
        private LiteDatabase _liteDb;

        public LiteDbDataService(ILiteDbContext liteDbContext)
        {
            _liteDb = liteDbContext.Database;
            var mapper = BsonMapper.Global;

            mapper.RegisterType
            (
                serialize: x =>
                {
                    if (x.IsRandomEveryMatch) return nameof(RaceOrRandom.RandomEveryMatch);
                    if (x.IsRandomOnTournament) return nameof(RaceOrRandom.RandomOnTournament);
                    if (x.IsRace)
                    {
                        var race = ((RaceOrRandom.Race)x).Item;
                        return race.ToString();
                    }
                    return string.Empty;
                },
                deserialize: x => {
                    switch (x.AsString)
                    {
                        case nameof(Race.Orks):
                            return RaceOrRandom.NewRace(Race.Orks);
                        case nameof(Race.Eldar):
                            return RaceOrRandom.NewRace(Race.Eldar);
                        case nameof(Race.Chaos):
                            return RaceOrRandom.NewRace(Race.Chaos);
                        case nameof(Race.ImperialGuard):
                            return RaceOrRandom.NewRace(Race.ImperialGuard);
                        case nameof(Race.Necrons):
                            return RaceOrRandom.NewRace(Race.Necrons);
                        case nameof(Race.DarkEldar):
                            return RaceOrRandom.NewRace(Race.DarkEldar);
                        case nameof(Race.SpaceMarines):
                            return RaceOrRandom.NewRace(Race.SpaceMarines);
                        case nameof(Race.Tau):
                            return RaceOrRandom.NewRace(Race.Tau);
                        case nameof(Race.SisterOfBattle):
                            return RaceOrRandom.NewRace(Race.SisterOfBattle);
                        case nameof(RaceOrRandom.RandomEveryMatch):
                            return RaceOrRandom.RandomEveryMatch;
                        case nameof(RaceOrRandom.RandomOnTournament):
                            return RaceOrRandom.RandomOnTournament;
                        default:
                            return RaceOrRandom.RandomEveryMatch;
                    }
                }
            );

            mapper.Entity<UserData>().Id(x => x.DiscordId, false);
        }

        public void AddPenalty(ulong discordId, int penalty)
        {
            var col = _liteDb.GetCollection<UserData>();
            var user = col.FindOne(x => x.DiscordId == discordId);

            if (user == null)
                return;

            user.Penalties += penalty;
            col.Update(user);
        }

        public UserData FindUserByDiscordId(ulong discordId)
        {
            var col = _liteDb.GetCollection<UserData>();
            return col.FindOne(x => x.DiscordId == discordId);
        }

        public IEnumerable<UserData> EnumerateAllUsers()
        {
            return _liteDb.GetCollection<UserData>().FindAll();
        }

        public UserData[] LoadAllsUsersWithScore()
        {
            return _liteDb.GetCollection<UserData>().Query()
              .Where(x => x.Score != 0)
              .ToArray();
        }

        public UserData[] LoadLeaders()
        {
            return _liteDb.GetCollection<UserData>().Query()
                .Where(x => x.Score != 0)
                .OrderByDescending(x => x.Score)
                .Limit(20)
                .ToArray();
        }

        public UserData FindUserBySteamId(ulong steamId)
        {
            return _liteDb.GetCollection<UserData>().FindOne(x => x.SteamId == steamId);
        }

        public void StoreTournamentAndIncrementTournamentId(TournamentData data)
        {
            _liteDb.GetCollection<TournamentData>().Insert(data);

            UpdateGlobals(x =>
            {
                x.CurrentTournamentId++;
                x.MatchesPlayed += data.Matches.Count(x => x.Result.IsWinner);
                x.EarnedRating += data.EarnedRating;
            });
        }

        private void UpdateGlobals(Action<GlobalData> update)
        {
            var globals = GetGlobals();
            update(globals);
            _liteDb.GetCollection<GlobalData>().Upsert(0, globals);
        }

        public bool StoreUsersSteamId(ulong discordId, ulong steamId)
        {
            var col = _liteDb.GetCollection<UserData>();

            col.EnsureIndex(x => x.SteamId);

            var userBySteam = col.FindOne(x => x.SteamId == steamId);

            if (userBySteam != null)
                return false;

            var userByDiscordId = col.FindOne(x => x.DiscordId == discordId) ?? new UserData();

            userByDiscordId.DiscordId = discordId;
            userByDiscordId.SteamId = steamId;

            return col.Upsert(userByDiscordId);
        }

        public bool UpdateUser(UserData userData)
        {
            return _liteDb.GetCollection<UserData>().Update(userData);
        }

        public bool DeleteUser(ulong discordId)
        {
            return _liteDb.GetCollection<UserData>().DeleteMany(x => x.DiscordId == discordId) > 0;
        }

        public (int SeasonId, int TournamentId) GetCurrentTournamentIds()
        {
            var globals = GetGlobals();
            return (globals.CurrentSeasonId, globals.CurrentTournamentId);
        }

        private GlobalData GetGlobals()
        {
            var col = _liteDb.GetCollection<GlobalData>();
            var globals = col.FindById(0);

            if (globals == null)
            {
                globals = new GlobalData();
                col.Upsert(0, globals);
            }

            return globals;
        }

        public void IncrementTournamentId()
        {
            UpdateGlobals(x => x.CurrentTournamentId++);
        }
    }
}
