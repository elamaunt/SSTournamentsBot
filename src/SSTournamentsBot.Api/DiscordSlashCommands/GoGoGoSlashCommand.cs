using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Services;
using SSTournamentsBot.Api.Threading;
using System;
using System.Threading.Tasks;
using static SSTournaments.Domain;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.DiscordSlashCommands
{
    public class GoGoGoSlashCommand : SlashCommandBase
    {
        readonly IBotApi _botApi;
        readonly TournamentEventsOptions _options;
        DateTime? _lastCallDate;

        public override string Name => "gogogo";

        public override string Description => $"Позвать всех на турнир (не чаще одного раза за {_options.CallToPlayTimeoutHours} часов)";

        readonly AsyncQueue _queue = new AsyncQueue();


        public GoGoGoSlashCommand(IBotApi botApi, IOptions<TournamentEventsOptions> options)
        {
            _botApi = botApi;
            _options = options.Value;
        }

        public override async Task Handle(SocketSlashCommand arg)
        {
            await arg.DeferAsync();

            await _queue.Async(async () =>
            {
                var last = _lastCallDate;
                var current = GetMoscowTime();

                if (!last.HasValue || (current - last.Value).TotalHours > _options.CallToPlayTimeoutHours)
                {
                    await arg.ModifyOriginalResponseAsync(x => x.Content = "> Запрос выполнен!");
                    await CallForTournament();
                }
                else
                {
                    await arg.ModifyOriginalResponseAsync(x => x.Content = $"> Сейчас нельзя позвать всех, так как с прошлого призыва прошло менее {_options.CallToPlayTimeoutHours} часов.");
                }
            });
        }

        private async Task CallForTournament()
        {
            _lastCallDate = GetMoscowTime();
            await _botApi.SendMessage("@everyone Призываю всех играть турниры!", GuildThread.TournamentChat);
        }
    }
}
