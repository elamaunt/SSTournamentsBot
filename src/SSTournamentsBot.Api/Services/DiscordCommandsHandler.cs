using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.DiscordSlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordCommandsHandler : IHostedService
    {
        readonly DiscordSocketClient _client;
        readonly DiscordBotOptions _options;
        readonly Dictionary<string, SlashCommandBase> _commands;

        private volatile bool _firstReadyRecieved;
        public DiscordCommandsHandler(
            DiscordSocketClient client,
            TournamentApi api,
            IDataService dataService,
            IStatsApi statsApi,
            IBotApi botApi,
            ITournamentEventsHandler eventsHandler,
            IEventsTimeline timeline,
            IOptions<DiscordBotOptions> options)
        {
            _client = client;
            _options = options.Value;

            _commands = new SlashCommandBase[]
            {
                new AddBotsSlashCommand(api),
                new CallSlashCommand(client, dataService, botApi, api),
                new CheckInSlashCommand(dataService, timeline, api),
                new ChekInBotsSlashCommand(api),
                new InfoSlashCommand(api),
                new KickBotsSlashCommand(api),
                new KickPlayerSlashCommand(dataService, eventsHandler, api),
                new LeaveSlashCommand(dataService, eventsHandler, api),
                new MyIdSlashCommand(),
                new PlayersShashCommand(api),
                new PlaySlashCommand(dataService, statsApi, api),
                new StatusSlashCommand(),
                new TimelineSlashCommand(timeline),
                new TimeSlashCommand(timeline),
                new ViewSlashCommand(api),
                new StartSlashCommand(timeline, botApi, api),
               // new VoteAddTimeSlashCommand(api),
               // new VoteBanSlashCommand(api),
               // new VoteKickSlashCommand(api),
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
            if (_firstReadyRecieved)
                return;

            _firstReadyRecieved = true;

            foreach (var guild in _client.Guilds)
            {
                //await guild.DeleteApplicationCommandsAsync();
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

                        if (sameCommand.Description != cmd.Description)
                        {
                            await sameCommand.DeleteAsync();
                            await guild.CreateApplicationCommandAsync(cmd.MakeBuilder().Build());
                        }
                    }
                }

                foreach (var cmd in currentCommands)
                {
                    await cmd.DeleteAsync();
                }

#if DEBUG
                await guild.GetTextChannel(_options.TournamentThreadId).SendMessageAsync(@"Привет, друзья! 
SS Tournaments Bot к вашим услугам и готов устраивать для Вас автоматические турниры.
Тестовый режим активирован. Для регистрации на турнир используйте команду /play.");
#else
                await guild.GetTextChannel(_options.TournamentThreadId).SendMessageAsync(@"Привет, друзья! 
**SS Tournaments Bot** к вашим услугам и готов устраивать для Вас автоматические турниры.
Они организуются ежедневно в __**18:00 по Мск**__.
*Начало чекина за 15 минут до начала.* Генерация сетки ровно в 18, либо сразу после чекина всех игроков, успевайте :)
Для регистрации на турнир используйте команду **/play.**");
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
