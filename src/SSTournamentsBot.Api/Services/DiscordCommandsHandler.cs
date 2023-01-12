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
        readonly TournamentEventsOptions _tournamentOptions;
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
            IOptions<DiscordBotOptions> options,
            IOptions<TournamentEventsOptions> tournamentOptions)
        {
            _client = client;
            _options = options.Value;
            _tournamentOptions = tournamentOptions.Value;

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
                new PlaySlashCommand(dataService, statsApi, api, timeline, tournamentOptions),
                new StatusSlashCommand(),
                new TimelineSlashCommand(timeline),
                new TimeSlashCommand(timeline),
                new ViewSlashCommand(api),
                new StartSlashCommand(timeline, botApi, api, tournamentOptions),
                new DeleteUserDataSlashCommand(dataService),
                new AddTimeSlashCommand(timeline),
                new KickBotSlashCommand(api),
                new SubmitGameSlashCommand(api, timeline),
                new RatedUsersSlashCommand(botApi, dataService),
                new RefreshLeaderboardSlashCommand(botApi, dataService),
                new RefreshLeaderboardV2SlashCommand(botApi, dataService),
                new UserSlashCommand(dataService),
                new AllUsersSlashCommand(botApi, dataService),
                new SetUsersScoreSlashCommand(dataService),
                new RegisterUserSlashCommand(dataService),
                new RebuildCommandsSlashCommand(this),
                new WaitSlashCommand(botApi),
               // new VoteAddTimeSlashCommand(api),
               // new VoteBanSlashCommand(api),
               // new VoteKickSlashCommand(api),
                new MatchesSlashCommand(api)
            }.ToDictionary(x => x.Name);
        }

        public async Task RebuildCommands()
        {
            foreach (var guild in _client.Guilds)
            {
                await guild.DeleteApplicationCommandsAsync();
                await UpdateOrCreateCommandsForGuild(guild);
            }
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
                await UpdateOrCreateCommandsForGuild(guild);

#if !DEBUG
                await guild.GetTextChannel(_options.TournamentThreadId).SendMessageAsync($@"Привет, друзья! 
**SS Tournaments Bot** к вашим услугам и готов устраивать для Вас автоматические турниры.
Они автоматически организуются сразу по достижению **минимального количества участников 4**.
После этого сразу же начинается стадия чекина и идет __ровно 10 минут__.
За это время могут регистрироваться больше участников, но после чекина игроков должно быть не менее {_tournamentOptions.MinimumPlayersToStartCheckin}, чтобы начать турнир.
Как только турнир завершается, набор игроков начинается заново.
Для регистрации на турнир используйте команду **/play.
Удачной игры!**");
#endif
            }
        }

        private async Task UpdateOrCreateCommandsForGuild(SocketGuild guild)
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
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Ready -= OnClientReady;
            _client.SlashCommandExecuted -= OnClientSlashCommandExecuted;

            return Task.CompletedTask;
        }
    }
}
