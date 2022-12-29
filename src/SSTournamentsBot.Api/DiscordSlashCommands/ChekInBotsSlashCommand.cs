﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class ChekInBotsSlashCommand : SlashCommandBase
    {
        public override string Name => "checkin-bots";
        public override string Description => "Зачекинить всех ботов из турнира (для тестов)";

        readonly TournamentApi _tournamentApi;
        public ChekInBotsSlashCommand(TournamentApi tournamentApi)
        {
            _tournamentApi = tournamentApi;
        }

        public override async Task Handle(SocketSlashCommand arg)
        {
            var players = _tournamentApi.RegisteredPlayers;
            var count = (long?)arg.Data.Options.FirstOrDefault(x => x.Name == "count")?.Value ?? null;

            if (!count.HasValue)
                for (int i = 0; i < players.Length; i++)
                {
                    var p = players[i];
                    if (p.IsBot)
                        _tournamentApi.TryCheckInUser(p.SteamId);
                }
            else
                for (int i = 0; i < count.Value; i++)
                {
                    var p = players.Where(x => x.IsBot).ElementAtOrDefault(i);
                    if (p?.IsBot ?? false)
                        _tournamentApi.TryCheckInUser(p.SteamId);
                }

            await arg.RespondAsync("Все боты зачекинелись.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("count")
                    .WithDescription("Количество ботов")
                    .WithRequired(false)
                    .WithType(ApplicationCommandOptionType.Integer))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}