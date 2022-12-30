using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DataDomain
{
    public class UserData
    {
        public ulong DiscordId { get; set; }
        public ulong SteamId { get; set; }
        public RaceOrRandom Race { get; set; }
        public bool StatsVerified { get; set; }
        public bool Banned { get; set; }
        public bool VotingDisabled { get; set; }
        public bool HasLowPriority { get; set; }
    }
}
