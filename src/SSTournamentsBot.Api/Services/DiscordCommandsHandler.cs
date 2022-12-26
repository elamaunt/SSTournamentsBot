using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using SSTournamentsBot.Api.DiscordSlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordCommandsHandler : IHostedService
    {
        readonly DiscordSocketClient _client;

        readonly Dictionary<string, SlashCommandBase> _commands;

        public DiscordCommandsHandler(
            DiscordSocketClient client,
            TournamentApi api,
            IDataService dataService,
            IStatsApi statsApi,
            IEventsTimeline timeline)
        {
            _client = client;

            _commands = new SlashCommandBase[]
            {
                new AddBotsSlashCommand(api),
                new CallSlashCommand(client, api),
                new CheckInSlashCommand(dataService, timeline, api),
                new ChekInBotsSlashCommand(api),
                new CheckOpponentSlashCommand(),
                new InfoSlashCommand(api),
                new KickBotsSlashCommand(api),
                new LeaveSlashCommand(dataService, api),
                new MyIdSlashCommand(),
                new PlayersShashCommand(api),
                new PlaySlashCommand(dataService, statsApi, api),
                new StatusSlashCommand(),
                new TimelineSlashCommand(timeline),
                new TimeSlashCommand(timeline),
                new ViewSlashCommand(api),
                new VoteSlashCommand(),
                new MatchesSlashCommand(api)
            }.ToDictionary(x => x.Name);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _client.Ready += OnClientReady;
            _client.SlashCommandExecuted += OnClientSlashCommandExecuted;

            return Task.CompletedTask;
        }

        private async Task OnClientSlashCommandExecuted(SocketSlashCommand arg)
        {
            var user = arg.User;
            if (user == null || user.IsBot)
                return;

            if (_commands.TryGetValue(arg.CommandName, out var command))
            {
                try
                {
                    await command.Handle(arg);
                }
                catch (NotImplementedException)
                {
                    await arg.RespondAsync($"Данная команда еще не реализована.");
                }
            }
            else
            {
                await arg.RespondAsync($"Неизвестная команда '{arg.CommandName}'.");
            }
        }

        private async Task OnClientReady()
        {
            foreach (var guild in _client.Guilds)
            {
                var currentCommands = (await guild.GetApplicationCommandsAsync()).ToList();

                foreach (var cmd in _commands.Values)
                {
                    var sameCommand = currentCommands.FirstOrDefault(x => x.Name == cmd.Name);
                    if (sameCommand == null)
                    {
                        await guild.CreateApplicationCommandAsync(cmd.MakeBuilder().Build());
                    }
                    else
                    {
                        currentCommands.Remove(sameCommand);
                    }
                }

                foreach (var cmd in currentCommands)
                {
                    await cmd.DeleteAsync();
                }

#if DEBUG
                await (guild.Channels.First(x => x.Name == "основной") as SocketTextChannel).SendMessageAsync(@"Привет, друзья! 
SS Tournaments Bot к вашим услугам и готов устраивать для Вас автоматические турниры.
Тестовый режим активирован. Для регистрации на турнир используйте команду play.");
#else
                await guild.DefaultChannel.SendMessageAsync(@"Привет, друзья! 
SS Tournaments Bot к вашим услугам и готов устраивать для Вас автоматические турниры.
Они организуются ежедневно в 18:00 по Мск.
Окончание регистрации и начало чекина за 15 минут до начала. Генерация сетки ровно в 18, никаких исключений, успевайте :)
Для регистрации на турнир используйте команду play.");
#endif
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Ready -= OnClientReady;
            _client.SlashCommandExecuted -= OnClientSlashCommandExecuted;

            return Task.CompletedTask;
        }
    }
}
