using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using SSTournamentsBot.Api.Domain;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        readonly ConcurrentDictionary<GuildThread, (string, SocketTextChannel[])[]> _channels = new ConcurrentDictionary<GuildThread, (string, SocketTextChannel[])[]>();
        private long _idsCounter;

        public DiscordBotApi(DiscordSocketClient client, IOptions<DiscordBotOptions> options)
        {
            _options = options.Value;
            _client = client;
        }

        public async Task SendFile(Context context, byte[] file, string fileName, Text text, GuildThread thread)
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

        public async Task SendMessage(Context context, Text message, GuildThread thread, params ulong[] mentions)
        {
            if (mentions.Length == 0 && string.IsNullOrWhiteSpace(message))
                return;

            var messageBuilder = new StringBuilder();

            for (int i = 0; i < mentions.Length; i++)
            {
                var user = await _client.GetUserAsync(mentions[i]);
                messageBuilder.Append(user.Mention);
                messageBuilder.Append(' ');
            }

            if (mentions.Length > 0)
                messageBuilder.Append("\n");

            messageBuilder.Append(message);

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

        public async Task<IButtonsController> SendVotingButtons(Context context, Text message, VotingOption[] options, GuildThread thread, params ulong[] mentions)
        {
            var messageBuilder = new StringBuilder();

            for (int i = 0; i < mentions.Length; i++)
            {
                var user = await _client.GetUserAsync(mentions[i]);
                messageBuilder.Append(user.Mention);
                messageBuilder.Append(' ');
            }

            if (mentions.Length > 0)
                messageBuilder.Append("\n");

            messageBuilder.Append(message);

            var channelsByLocale = GetChannels(context, thread);
            var resultedMessage = messageBuilder.ToString();
            var builder = new ComponentBuilder();

            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                builder.WithButton(option.Message, i.ToString(), style: ConvertStyle(option.Style));
            }

            var restMessages = new List<RestUserMessage>(channelsByLocale.Sum(x => x.channels.Length));

            for (int i = 0; i < channelsByLocale.Length; i++)
            {
                var (locale, channels) = channelsByLocale[i];

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

        private (string locale, SocketTextChannel[] channels)[] GetChannels(Context context, GuildThread thread)
        {
            if (!_channels.TryGetValue(thread, out var channels))
            {
                var main = _client.GetGuild(_options.MainGuildId);

                var channelsList = new List<(string, SocketTextChannel[])>();

                /*if (thread.HasFlag(GuildThread.EventsTape))
                    channelsList.Add(main.GetTextChannel(_options.EventsThreadId));
                if (thread.HasFlag(GuildThread.History))
                    channelsList.Add(main.GetTextChannel(_options.HistoryThreadId));
                if (thread.HasFlag(GuildThread.Leaderboard))
                    channelsList.Add(main.GetTextChannel(_options.LeaderboardThreadId));
                if (thread.HasFlag(GuildThread.Logging))
                    channelsList.Add(main.GetTextChannel(_options.LoggingThreadId));
                if (thread.HasFlag(GuildThread.TournamentChat))
                    channelsList.Add(main.GetTextChannel(_options.TournamentThreadId));
                if (thread.HasFlag(GuildThread.VotingsTape))
                    channelsList.Add(main.GetTextChannel(_options.VotingsTapeThreadId));*/

                channels = _channels[thread] = channelsList.ToArray();
            }

            return channels;
        }

        public async Task ModifyLastMessage(Context context, Text message, GuildThread thread)
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
                        await channels[k].SendMessageAsync(message);
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

        public async Task SendMessageToUser(Context context, Text message, ulong id)
        {
            await (await _client.GetUserAsync(id)).SendMessageAsync(message);
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

            public Task DisableButtons(Text resultMessage)
            {
                return Task.WhenAll(_messages.Select(x => x.ModifyAsync(m =>
                {
                    m.Content = resultMessage.Build();
                    m.Components = null;
                })));
            }
        }
    }
}
