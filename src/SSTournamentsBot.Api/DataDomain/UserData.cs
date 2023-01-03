using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

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
        public MentionSetting MentionSetting { get; set; } = MentionSetting.Default;
    }
}
