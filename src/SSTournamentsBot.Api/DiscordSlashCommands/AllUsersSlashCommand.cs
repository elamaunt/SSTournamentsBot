﻿using Discord;
using Discord.WebSocket;
using SSTournamentsBot.Api.Resources;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class AllUsersSlashCommand : SlashCommandBase
    {
        public override string Name => "all-users";
        public override string DescriptionKey=> nameof(S.Commands_Users);

        readonly IDataService _dataService;
        public AllUsersSlashCommand(IDataService dataService)
        {
            _dataService = dataService;
        }

        public override async Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            var builder = new StringBuilder();

            int i = 1;

            foreach (var user in _dataService.EnumerateAllUsers())
            {
                builder.AppendLine($"{i++}. {user.Score} | {user.Penalties} | {user.SteamId} | {user.DiscordId} | **{await context.BotApi.GetUserName(context, user.DiscordId)}**");
            }

            if (builder.Length > 0)
                await arg.RespondAsync(builder.ToString());
            else
                await arg.RespondAsync("Нет данных.");
        }

        protected override void Configure(SlashCommandBuilder builder)
        {
            builder
                .WithDefaultPermission(true)
                .WithDefaultMemberPermissions(GuildPermission.Administrator | GuildPermission.ModerateMembers)
                .WithDMPermission(true);
        }
    }
}
