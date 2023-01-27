﻿using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class StatusSlashCommand : SlashCommandBase
    {
        public override string Name => "status";

        public override string Description => "Узнать ваш статус в текущем турнире";

        public override Task Handle(Context context, SocketSlashCommand arg, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
