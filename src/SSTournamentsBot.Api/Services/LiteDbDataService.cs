using LiteDB;
using SSTournamentsBot.Api.DataDomain;
using static SSTournaments.Domain;

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
                deserialize: x => { switch (x.AsString)
                    {
                        case "Orks":
                            return RaceOrRandom.NewRace(Race.Orks);
                        case "Eldar":
                            return RaceOrRandom.NewRace(Race.Eldar);
                        case "Chaos":
                            return RaceOrRandom.NewRace(Race.Chaos);
                        case "ImperialGuard":
                            return RaceOrRandom.NewRace(Race.ImperialGuard);
                        case "Necrons":
                            return RaceOrRandom.NewRace(Race.Necrons);
                        case "DarkEldar":
                            return RaceOrRandom.NewRace(Race.DarkEldar);
                        case "SpaceMarines":
                            return RaceOrRandom.NewRace(Race.SpaceMarines);
                        case "Tau":
                            return RaceOrRandom.NewRace(Race.Tau);
                        case "SisterOfBattle":
                            return RaceOrRandom.NewRace(Race.SisterOfBattle);
                        default:
                            return RaceOrRandom.RandomEveryMatch;
                    }
                }
            );

            mapper.Entity<UserData>()
             .Id(x => x.DiscordId);
        }

        public UserData FindUserByDiscordId(ulong discordId)
        {
            var col = _liteDb.GetCollection<UserData>();
            var user = col.FindOne(x => x.DiscordId == discordId);

            if (user == null)
                return null;

            return new UserData() 
            { 
                DiscordId = user.DiscordId,
                SteamId = user.SteamId,
                Race = user.Race
            };
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

        public void StoreUsersSteamId(ulong discordId, ulong steamId)
        {
            var col = _liteDb.GetCollection<UserData>();

            col.EnsureIndex(x => x.SteamId);

            var userBySteam = col.FindOne(x => x.SteamId == steamId);

            if (userBySteam != null && userBySteam.DiscordId == discordId)
                throw new System.Exception("Такой SteamId уже зарегистрирован на другого пользователя.");

            var userByDiscordId = col.FindOne(x => x.DiscordId == discordId) ?? new UserData();

            userByDiscordId.DiscordId = discordId;
            userByDiscordId.SteamId = steamId;

            col.Upsert(userByDiscordId);
        }

        public UserData UpdateUser(UserData userData)
        {
            if (_liteDb.GetCollection<UserData>().Update(userData))
                return userData;

            return FindUserBySteamId(userData.SteamId);
        }
    }
}
