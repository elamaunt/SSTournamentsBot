using LiteDB;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DataDomain
{
    public class UserData
    {
        public ulong DiscordId { get; set; }
        public ulong SteamId { get; set; }
        public RaceOrRandom Race { get; set; } = RaceOrRandom.RandomEveryMatch;
        public bool StatsVerified { get; set; }
        public bool Banned { get; set; }
        public bool VotingDisabled { get; set; }
        public bool HasLowPriority { get; set; }
        public int Score { get; set; }
        public int Penalties { get; set; }
        public int Map1v1BansRaw { get; set; }

        [BsonIgnore]
        public MapBans Map1v1Bans
        {
            get => (MapBans)Map1v1BansRaw;
            set => Map1v1BansRaw = (int)value;
        }
    }
}
