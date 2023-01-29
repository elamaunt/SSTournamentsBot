using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Helpers;
using SSTournamentsBot.Api.Resources;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordBot : IHostedService
    {
        readonly DiscordSocketClient _client;
        readonly ContextService _contextService;
        readonly DiscordBotOptions _options;
        readonly IEventsTimeline _timeLine;

        private volatile bool _isReady;

        public DiscordBot(DiscordSocketClient client,
            ContextService contextService,
            IEventsTimeline timeLine,
            IOptions<DiscordBotOptions> options)
        {
            _client = client;
            _contextService = contextService;
            _timeLine = timeLine;
            _options = options.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var context = _contextService.GetMainContext();

            _client.Log += async (msg) =>
            {
                var s = msg.ToString();
                Console.WriteLine(s);

                if (_isReady)
                {
                    if (msg.Severity == LogSeverity.Error || msg.Severity == LogSeverity.Critical)
                        await context.BotApi.SendMessage(context, Text.OfValue(s), GuildThread.Logging, 272710324484833281);
                    else
                        await context.BotApi.SendMessage(context, Text.OfValue(s), GuildThread.Logging);
                }
            };

            _client.Ready += OnReady;
            _client.ButtonExecuted += OnButtonExecuted;

            await _client.LoginAsync(TokenType.Bot, _options.Token);
            await _client.StartAsync();
        }

        private Task OnReady()
        {
            _isReady = true;
            return Task.CompletedTask;
        }

        private async Task OnButtonExecuted(SocketMessageComponent arg)
        {
            var (locale, context) = _contextService.GetLocaleAndContext(arg.Channel.Id);

            var guildUser = (SocketGuildUser)arg.User;

            var guildRole = GuildRole.Everyone;
            if (guildUser.GuildPermissions.KickMembers)
                guildRole = GuildRole.Moderator;
            if (guildUser.GuildPermissions.Administrator)
                guildRole = GuildRole.Administrator;

            var result = await context.TournamentApi.TryAcceptVote(arg.User.Id, int.Parse(arg.Data.CustomId), guildRole);
            var culture = CultureInfo.GetCultureInfo(locale);

            if (result == AcceptVoteResult.Accepted)
            {
                await arg.RespondAsync(Text.OfKey(nameof(S.AcceptVote_Accepted)).Format(arg.User.Mention).Build(culture));
                return;
            }

            if (result == AcceptVoteResult.NoVoting)
            {
                await arg.RespondAsync(Text.OfKey(nameof(S.AcceptVote_NoVoting)).Format(arg.User.Mention).Build(culture));
                return;
            }

            if (result == AcceptVoteResult.TheVoteIsOver)
            {
                await arg.RespondAsync(Text.OfKey(nameof(S.AcceptVote_VotingEnded)).Format(arg.User.Mention).Build(culture));
                return;
            }

            if (result == AcceptVoteResult.YouCanNotVote)
            {
                await arg.RespondAsync(Text.OfKey(nameof(S.AcceptVote_NoPermission)).Format(arg.User.Mention).Build(culture));
                return;
            }

            if (result == AcceptVoteResult.AlreadyVoted)
            {
                await arg.RespondAsync(Text.OfKey(nameof(S.AcceptVote_AlreadyAccepted)).Format(arg.User.Mention).Build(culture));
                return;
            }

            if (result == AcceptVoteResult.CompletedByThisVote)
            {
                await arg.RespondAsync(Text.OfKey(nameof(S.AcceptVote_ForcedByAdmin)).Format(arg.User.Mention).Build(culture));
                await context.EventsHandler.DoCompleteVoting(context.Name);
                return;
            }

            await arg.RespondAsync(Text.OfKey(nameof(S.AcceptVote_Error)).Format(arg.User.Mention).Build(culture));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Ready -= OnReady;
            _client.ButtonExecuted -= OnButtonExecuted;
            _isReady = false;
            return _client.StopAsync();
        }
    }
}
