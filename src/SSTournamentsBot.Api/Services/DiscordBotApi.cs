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

        readonly ConcurrentDictionary<GuildThread, (CultureInfo, SocketTextChannel[], SocketTextChannel[])[]> _channels = new ConcurrentDictionary<GuildThread, (CultureInfo, SocketTextChannel[], SocketTextChannel[])[]>();
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

            var mutualChannelMessages = new Dictionary<SocketTextChannel, MemoryStream>();
            var channelsByLocale = GetChannels(context, thread);

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels, mutualChannels) = channelsByLocale[i];

                for (int k = 0; k < channels.Length; k++)
                {
                    var ch = channels[k];

                    if (mutualChannels.Contains(ch))
                    {
                        if (!mutualChannelMessages.ContainsKey(ch))
                            mutualChannelMessages.Add(ch, new MemoryStream(file));
                    }
                    else
                        await ch.SendFileAsync(new MemoryStream(file), fileName ?? "Image", text?.Build() ?? "Image");
                }
            }

            foreach (var item in mutualChannelMessages)
            {
                await item.Key.SendFileAsync(item.Value, fileName ?? "Image", "");
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

            var mutualChannelMessages = new Dictionary<SocketTextChannel, StringBuilder>();

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels, mutualChannels) = channelsByLocale[i];

                for (int k = 0; k < channels.Length; k++)
                {
                    var ch = channels[k];

                    if (mutualChannels.Contains(ch))
                    {
                        if (!mutualChannelMessages.ContainsKey(ch))
                            mutualChannelMessages.Add(ch, messageBuilder);
                    }
                    else
                        await ch.SendMessageAsync(resultedMessage);
                }
            }

            foreach (var item in mutualChannelMessages)
            {
                await item.Key.SendMessageAsync(item.Value.ToString());
            }
        }

        public async Task SendMessage(Context context, IText message, GuildThread thread, params ulong[] mentions)
        {
            if (mentions.Length == 0 && string.IsNullOrWhiteSpace(message.Build()))
                return;

            var channelsByLocale = GetChannels(context, thread);
            var mutualChannelMessages = new Dictionary<SocketTextChannel, StringBuilder>();

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels, mutualChannels) = channelsByLocale[i];
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
                    var ch = channels[k];

                    if (mutualChannels.Contains(ch))
                    {
                        if (!mutualChannelMessages.TryGetValue(ch, out var builder))
                        {
                            mutualChannelMessages.Add(ch, messageBuilder);
                        }
                        else
                        {
                            builder.AppendLine(message.Build(locale));
                        }
                    }
                    else
                        await ch.SendMessageAsync(resultedMessage);
                }
            }

            foreach (var item in mutualChannelMessages)
            {
                await item.Key.SendMessageAsync(item.Value.ToString());
            }
        }

        public async Task<IButtonsController> SendVotingButtons(Context context, IText message, VotingOption[] options, GuildThread thread, params ulong[] mentions)
        {
            var channelsByLocale = GetChannels(context, thread);
            var restMessages = new List<RestUserMessage>(channelsByLocale.Sum(x => x.channels.Length));
            var mutualChannelMessages = new Dictionary<SocketTextChannel, StringBuilder>();

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels, mutualChannels) = channelsByLocale[i];
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
                    var ch = channels[k];

                    if (mutualChannels.Contains(ch))
                    {
                        if (!mutualChannelMessages.TryGetValue(ch, out var savedBuilder))
                        {
                            mutualChannelMessages.Add(ch, messageBuilder);
                        }
                        else
                        {
                            savedBuilder.AppendLine(message.Build(locale));
                        }
                    }
                    else
                        restMessages.Add(await ch.SendMessageAsync(resultedMessage, components: builder.Build()));
                }
            }

            foreach (var item in mutualChannelMessages)
            {
                restMessages.Add(await item.Key.SendMessageAsync(item.Value.ToString()));
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

        private (CultureInfo locale, SocketTextChannel[] channels, SocketTextChannel[] mutualChannels)[] GetChannels(Context context, GuildThread thread)
        {
            if (!_channels.TryGetValue(thread, out var channels))
            {
                var main = _client.GetGuild(_options.MainGuildId);

                var channelsListWithLocale = new List<(CultureInfo, SocketTextChannel[], SocketTextChannel[])>();

                foreach (var pair in context.Options.Channels)
                {
                    var mutualChannelsList = new List<SocketTextChannel>();
                    var channelsList = new List<SocketTextChannel>();
                    var scope = pair.Value;

                    var otherOptions = context.Options.Channels.Where(x => x.Key != pair.Key);

                    if (thread.HasFlag(GuildThread.EventsTape) && scope.EventsThreadId != 0)
                    {
                        var channel = main.GetTextChannel(scope.EventsThreadId);
                        channelsList.Add(channel);
                        if (otherOptions.Any(x => x.Value.EventsThreadId == scope.EventsThreadId))
                            mutualChannelsList.Add(channel);
                    }

                    if (thread.HasFlag(GuildThread.History) && scope.HistoryThreadId != 0)
                    {
                        var channel = main.GetTextChannel(scope.HistoryThreadId);
                        channelsList.Add(channel);
                        if (otherOptions.Any(x => x.Value.HistoryThreadId == scope.HistoryThreadId))
                            mutualChannelsList.Add(channel);
                    }

                    if (thread.HasFlag(GuildThread.Leaderboard) && scope.LeaderboardThreadId != 0)
                    {
                        var channel = main.GetTextChannel(scope.LeaderboardThreadId);
                        channelsList.Add(channel);
                        if (otherOptions.Any(x => x.Value.LeaderboardThreadId == scope.LeaderboardThreadId))
                            mutualChannelsList.Add(channel);
                    }

                    if (thread.HasFlag(GuildThread.Logging) && scope.LoggingThreadId != 0)
                    {
                        var channel = main.GetTextChannel(scope.LoggingThreadId);
                        channelsList.Add(channel);
                        if (otherOptions.Any(x => x.Value.LoggingThreadId == scope.LoggingThreadId))
                            mutualChannelsList.Add(channel);
                    }

                    if (thread.HasFlag(GuildThread.TournamentChat) && scope.TournamentThreadId != 0)
                    {
                        var channel = main.GetTextChannel(scope.TournamentThreadId);
                        channelsList.Add(channel);
                        if (otherOptions.Any(x => x.Value.TournamentThreadId == scope.TournamentThreadId))
                            mutualChannelsList.Add(channel);
                    }

                    if (thread.HasFlag(GuildThread.VotingsTape) && scope.VotingsTapeThreadId != 0)
                    {
                        var channel = main.GetTextChannel(scope.VotingsTapeThreadId);
                        channelsList.Add(channel);
                        if (otherOptions.Any(x => x.Value.VotingsTapeThreadId == scope.VotingsTapeThreadId))
                            mutualChannelsList.Add(channel);
                    }

                    channelsListWithLocale.Add((CultureInfo.GetCultureInfo(pair.Key), channelsList.ToArray(), mutualChannelsList.ToArray()));
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
                var (locale, channels, mutualChannels) = channelsByLocale[i];

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
            var user = await _client.GetUserAsync(id);

            await user.SendMessageAsync(message.Build(CultureInfo.GetCultureInfo("ru")));
            await user.SendMessageAsync(message.Build(CultureInfo.GetCultureInfo("en")));
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
