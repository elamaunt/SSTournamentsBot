﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class KickBotSlashCommand : SlashCommandBase
    {
        public override string Name => "kick-bot";
        public override string DescriptionKey=> nameof(S.Commands_KickBot);

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var botIdOption = arg.Data.Options.FirstOrDefault(x => x.Name == "bot-id");
            var reasonOption = arg.Data.Options.FirstOrDefault(x => x.Name == "reason");
            var players = context.TournamentApi.RegisteredPlayers;

            var id = (ulong)(long)botIdOption.Value;
            TechnicalWinReason reason;

            switch ((long)reasonOption.Value)
            {
                case 0:
                    reason = TechnicalWinReason.OpponentsLeft;
                    break;
                case 1:
                    reason = TechnicalWinReason.OpponentsKicked;
                    break;
                case 2:
                    reason = TechnicalWinReason.OpponentsBan;
                    break;
                case 3:
                    reason = TechnicalWinReason.Voting;
                    break;
                default:
                    reason = TechnicalWinReason.OpponentsLeft;
                    break;

            }

            if ((await context.TournamentApi.TryLeaveUser(id, id, reason)).IsDone)
                await arg.RespondAsync("Бот исключен.");
            else
                await arg.RespondAsync("Не удалось исключить бота.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("bot-id")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithDescription("Id пользователя"))
                    
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("reason")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.Integer)
                    .WithDescription("Id причины"))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
