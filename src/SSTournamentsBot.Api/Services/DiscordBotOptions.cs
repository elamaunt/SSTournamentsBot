using System.Collections.Generic;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordBotOptions
    {
        public string Token { get; set; }
        public ulong MainGuildId { get; set; }
        public ulong WaitingRoleId { get; set; }
        public Dictionary<string, ulong> MainThreads { get; set; }
        public Dictionary<string, SetupOptions> Setups { get; set; }
    }

    public class SetupOptions
    {
        public string Mod { get; set; }
        public SetupType Type { get; set; }
        public Dictionary<string, string> Maps { get; set; }
        public Dictionary<string, ChannelScope> Channels { get; set; }
    }

    public class ChannelScope
    {
        public ulong MainThreadId { get; set; }
        public ulong EventsThreadId { get; set; }
        public ulong HistoryThreadId { get; set; }
        public ulong LeaderboardThreadId { get; set; }
        public ulong LoggingThreadId { get; set; }
        public ulong VotingsTapeThreadId { get; set; }
    }
}
