﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class BanPlayerSlashCommand : SlashCommandBase
    {
        public override string Name => "ban-player";
        public override string DescriptionKey=> nameof(S.Commands_BanPlayer);

        readonly IDataService _dataService;

        public BanPlayerSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var userOption = arg.Data.Options.FirstOrDefault(x => x.Name == "player");

            var user = (IUser)userOption.Value;
            var userData = _dataService.FindUserByDiscordId(user.Id);

            if (userData == null)
            {
                await arg.RespondAsync("Пользователь не зарегистрирован в системе.");
                return;
            }

            if ((await context.TournamentApi.TryLeaveUser(userData.DiscordId, userData.SteamId, TechnicalWinReason.OpponentsBan)).IsDone)
            {
                var mention = await context.BotApi.GetMention(context, userData.DiscordId);
                await context.BotApi.SendMessage(context, OfKey(nameof(S.BanPlayer_Kicked)).Format(mention), GuildThread.EventsTape | GuildThread.TournamentChat);
            }

            userData.Banned = true;
            _dataService.UpdateUser(userData);
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder.WithDefaultPermission(true)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("player")
                    .WithRequired(true)
                    .WithType(ApplicationCommandOptionType.User))
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .WithDMPermission(true);
        }
    }
}
