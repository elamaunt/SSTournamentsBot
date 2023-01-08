using LiteDB;
using SSTournamentsBot.Api.DataDomain;
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
                serialize: x => x.ToString(),
                deserialize: x => {
                    switch (x.AsString)
                    {
                        case nameof(MentionSetting.Default):
                            return MentionSetting.Default;
                        case nameof(MentionSetting.OnlyCheckin):
                            return MentionSetting.OnlyCheckin;
                        default:
                            return MentionSetting.Default;
                    }
                }
            );

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

            mapper.Entity<UserData>().Id(x => x.DiscordId);
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

        public void StoreTournament(TournamentData data)
        {
            _liteDb.GetCollection<TournamentData>().Insert(data);
        }

        public bool StoreUsersSteamId(ulong discordId, ulong steamId)
        {
            var col = _liteDb.GetCollection<UserData>();

            col.EnsureIndex(x => x.SteamId);

            var userBySteam = col.FindOne(x => x.SteamId == steamId);

            if (userBySteam != null && userBySteam.DiscordId == discordId)
                return false;

            var userByDiscordId = col.FindOne(x => x.DiscordId == discordId) ?? new UserData();

            userByDiscordId.DiscordId = discordId;
            userByDiscordId.SteamId = steamId;

            return col.Upsert(userByDiscordId);
        }

        public UserData UpdateUser(UserData userData)
        {
            if (_liteDb.GetCollection<UserData>().Update(userData))
                return userData;

            return FindUserBySteamId(userData.SteamId);
        }

        public bool DeleteUser(ulong discordId)
        {
            return _liteDb.GetCollection<UserData>().DeleteMany(x => x.DiscordId == discordId) > 0;
        }
    }
}
