using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordBotApi : IBotApi
    {
        readonly DiscordSocketClient _client;
        readonly DiscordBotOptions _options;

        readonly ConcurrentDictionary<GuildThread, (CultureInfo, SocketTextChannel[])[]> _channels = new ConcurrentDictionary<GuildThread, (CultureInfo, SocketTextChannel[])[]>();
        private long _idsCounter;

        public DiscordBotApi(DiscordSocketClient client, IOptions<DiscordBotOptions> options)
        {
            _options = options.Value;
            _client = client;
        }

        public async Task SendFile(Context context, byte[] file, string fileName, IText text, GuildThread thread)
        {
            if (file == null || file.Length == 0)
                return;

            var channelsByLocale = GetChannels(context, thread);

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels) = channelsByLocale[i];

                for (int k = 0; k < channels.Length; k++)
                {
                    await channels[k].SendFileAsync(new MemoryStream(file), fileName ?? "Image", text?.Build() ?? "Image");
                }
            }
        }

        public async Task Mention(Context context, GuildThread thread, params ulong[] mentions)
        {
            if (mentions.Length == 0)
                return;

            var messageBuilder = new StringBuilder();

            for (int i = 0; i < mentions.Length; i++)
            {
                var user = await _client.GetUserAsync(mentions[i]);
                messageBuilder.Append(user.Mention);
                messageBuilder.Append(' ');
            }

            var channelsByLocale = GetChannels(context, thread);
            var resultedMessage = messageBuilder.ToString();

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels) = channelsByLocale[i];

                for (int k = 0; k < channels.Length; k++)
                {
                    await channels[k].SendMessageAsync(resultedMessage);
                }
            }
        }

        public async Task SendMessage(Context context, IText message, GuildThread thread, params ulong[] mentions)
        {
            if (mentions.Length == 0 && string.IsNullOrWhiteSpace(message.Build()))
                return;

            var channelsByLocale = GetChannels(context, thread);

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels) = channelsByLocale[i];
                var messageBuilder = new StringBuilder();

                for (int n = 0; n < mentions.Length; n++)
                {
                    var user = await _client.GetUserAsync(mentions[n]);
                    messageBuilder.Append(user.Mention);
                    messageBuilder.Append(' ');
                }

                if (mentions.Length > 0)
                    messageBuilder.Append("\n");

                messageBuilder.Append(message.Build(locale));

                var resultedMessage = messageBuilder.ToString();

                for (int k = 0; k < channels.Length; k++)
                {
                    await channels[k].SendMessageAsync(resultedMessage);
                }
            }
        }

        public async Task<IButtonsController> SendVotingButtons(Context context, IText message, VotingOption[] options, GuildThread thread, params ulong[] mentions)
        {
            var channelsByLocale = GetChannels(context, thread);
            var restMessages = new List<RestUserMessage>(channelsByLocale.Sum(x => x.channels.Length));

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels) = channelsByLocale[i];
                var messageBuilder = new StringBuilder();

                for (int m = 0; m < mentions.Length; m++)
                {
                    var user = await _client.GetUserAsync(mentions[m]);
                    messageBuilder.Append(user.Mention);
                    messageBuilder.Append(' ');
                }

                if (mentions.Length > 0)
                    messageBuilder.Append("\n");

                messageBuilder.Append(message.Build(locale));

                var resultedMessage = messageBuilder.ToString();
                var builder = new ComponentBuilder();

                for (int n = 0; n < options.Length; n++)
                {
                    var option = options[n];
                    builder.WithButton(option.Message, n.ToString(), style: ConvertStyle(option.Style));
                }

                for (int k = 0; k < channels.Length; k++)
                {
                    restMessages.Add(await channels[k].SendMessageAsync(resultedMessage, components: builder.Build()));
                }
            }

            return new DiscordButtonsController(Interlocked.Increment(ref _idsCounter), restMessages.ToArray());
        }

        private ButtonStyle ConvertStyle(BotButtonStyle style)
        {
            if (style.IsPrimary) return ButtonStyle.Primary;
            if (style.IsSecondary) return ButtonStyle.Secondary;
            if (style.IsSuccess) return ButtonStyle.Success;
            if (style.IsDanger) return ButtonStyle.Danger;
            if (style.IsLink) return ButtonStyle.Link;
            return ButtonStyle.Secondary;
        }

        private (CultureInfo locale, SocketTextChannel[] channels)[] GetChannels(Context context, GuildThread thread)
        {
            if (!_channels.TryGetValue(thread, out var channels))
            {
                var main = _client.GetGuild(_options.MainGuildId);

                var channelsListWithLocale = new List<(CultureInfo, SocketTextChannel[])>();

                foreach (var pair in context.Options.Channels)
                {
                    var channelsList = new List<SocketTextChannel>();
                    var scope = pair.Value;

                    if (thread.HasFlag(GuildThread.EventsTape))
                        channelsList.Add(main.GetTextChannel(scope.EventsThreadId));
                    if (thread.HasFlag(GuildThread.History))
                        channelsList.Add(main.GetTextChannel(scope.HistoryThreadId));
                    if (thread.HasFlag(GuildThread.Leaderboard))
                        channelsList.Add(main.GetTextChannel(scope.LeaderboardThreadId));
                    if (thread.HasFlag(GuildThread.Logging))
                        channelsList.Add(main.GetTextChannel(scope.LoggingThreadId));
                    if (thread.HasFlag(GuildThread.TournamentChat))
                        channelsList.Add(main.GetTextChannel(scope.TournamentThreadId));
                    if (thread.HasFlag(GuildThread.VotingsTape))
                        channelsList.Add(main.GetTextChannel(scope.VotingsTapeThreadId));

                    channelsListWithLocale.Add((CultureInfo.GetCultureInfo(pair.Key), channelsList.ToArray()));
                }

                channels = _channels[thread] = channelsListWithLocale.ToArray();
            }

            return channels;
        }

        public async Task ModifyLastMessage(Context context, IText message, GuildThread thread)
        {
            var channelsByLocale = GetChannels(context, thread);

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels) = channelsByLocale[i];

                for (int k = 0; k < channels.Length; k++)
                {
                    var lastMessages = channels[k].GetMessagesAsync(2);

                    var page = await lastMessages.FirstOrDefaultAsync();
                    var messageEntry = page.FirstOrDefault(x => x.Author.IsBot);

                    if (messageEntry != null)
                    {
                        await channels[k].ModifyMessageAsync(messageEntry.Id, x =>
                        {
                            x.Content = message.Build();
                        });
                    }
                    else
                    {
                        await channels[k].SendMessageAsync(message.Build());
                    }
                }
            }
        }

        public async Task<string> GetUserName(Context context, ulong id)
        {
            return (await _client.GetUserAsync(id)).Username;
        }

        public async Task<string> GetMention(Context context, ulong id)
        {
            return (await _client.GetUserAsync(id)).Mention;
        }

        public async Task SendMessageToUser(Context context, IText message, ulong id)
        {
            await (await _client.GetUserAsync(id)).SendMessageAsync(message.Build());
        }

        public async Task<bool> ToggleWaitingRole(Context context, ulong id, bool? toValue)
        {
            var main = _client.GetGuild(_options.MainGuildId);
            var waitingRole = main.GetRole(_options.WaitingRoleId);
            var user = main.GetUser(id);

            if (user.Roles.Any(x => x.Id == _options.WaitingRoleId))
            {
                if (!toValue.HasValue || !toValue.Value)
                {
                    await user.RemoveRoleAsync(waitingRole);
                    return false;
                }

                return true;
            }
            else
            {
                if (!toValue.HasValue || toValue.Value)
                {
                    await user.AddRoleAsync(waitingRole);
                    return true;
                }

                return false;
            }
        }

        public Task<string> GetMentionForWaitingRole(Context context)
        {
            var mainGuild = _client.Guilds.First();
            return Task.FromResult(mainGuild.GetRole(_options.WaitingRoleId).Mention);
        }

        public async Task MentionWaitingRole(Context context, GuildThread thread)
        {
            var mention = await GetMentionForWaitingRole(context);
            await SendMessage(context, Text.OfValue(mention), thread);
        }

        private class DiscordButtonsController : IButtonsController
        {
            private readonly RestUserMessage[] _messages;
            public long Id { get; }

            public DiscordButtonsController(long id, RestUserMessage[] messages)
            {
                Id = id;
                _messages = messages;
            }

            public Task<bool> ContainsMessageId(ulong id)
            {
                return Task.FromResult(_messages.Any(x => x.Id == id));
            }

            public Task DisableButtons(Text resultMessage, Func<ulong, CultureInfo> cultureSelector)
            {
                return Task.WhenAll(_messages.Select(x => x.ModifyAsync(m =>
                {
                    m.Content = resultMessage.Build(cultureSelector(x.Channel.Id));
                    m.Components = null;
                })));
            }
        }
    }
}
