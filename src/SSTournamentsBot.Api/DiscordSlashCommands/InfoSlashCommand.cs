﻿using Discord.WebSocket;
using SSTournamentsBot.Api.Services;
using System.Text;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class InfoSlashCommand : SlashCommandBase
    {
        readonly TournamentApi _api;

        public InfoSlashCommand(TournamentApi api)
        {
            _api = api;
        }

        public override string Name => "info";

        public override string Description => "Получить текущую информацию о следующем турнире";

        public override async Task Handle(Context context, SocketSlashCommand arg)
        {
            var infoBuilder = new StringBuilder();

            if (_api.IsTournamentStarted)
            {
                infoBuilder.AppendLine(">>> В данный момент идет турнир.");
            }
            else
            {
                infoBuilder.AppendLine(">>> Турнир еще не начался.");
            }

            infoBuilder.AppendLine("Тип турнира: " + (_api.TournamentType?.ToString() ?? "Неопределен"));

            var players = _api.RegisteredPlayers;

            if (players.Length == 0)
                infoBuilder.AppendLine("Пока никто не зарегистрировался, но, возможно, вы будете первым :)");
            else
                infoBuilder.AppendLine("Зарегистрировано участников: " + players.Length);

            await arg.RespondAsync(infoBuilder.ToString());
        }
    }
}
