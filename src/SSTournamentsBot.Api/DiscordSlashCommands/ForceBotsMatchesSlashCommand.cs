using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ForceBotsMatchesSlashCommand : SlashCommandBase
    {
        readonly TournamentApi _api;

        public override string Name => "force-bots-matches";
        public override string Description => "Завершает активные матчи с ботами проигрышем одного из ботов (для тестов)";

        public ForceBotsMatchesSlashCommand(TournamentApi api)
        {
            _api = api;
        }

        public override async Task Handle(SocketSlashCommand arg)
        {
            var random = new Random();
            var matches = _api.ActiveMatches;

            foreach (var match in matches)
            {
                var p1 = match.Player1.Value.Item1;
                var p2 = match.Player2.Value.Item1;
                if (random.Next(2) == 0 && p1.IsBot)
                {
                    await _api.TryLeaveUser(p1.DiscordId, p1.SteamId);
                } else if (p2.IsBot)
                {
                    await _api.TryLeaveUser(p2.DiscordId, p2.SteamId);
                }
                else if (p1.IsBot)
                {
                    await _api.TryLeaveUser(p1.DiscordId, p1.SteamId);
                }
            }
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
