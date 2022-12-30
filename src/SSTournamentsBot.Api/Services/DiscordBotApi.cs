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

        readonly ConcurrentDictionary<GuildThread, SocketTextChannel[]> _channels = new ConcurrentDictionary<GuildThread, SocketTextChannel[]>();
        private long _idsCounter;

        public DiscordBotApi(DiscordSocketClient client, IOptions<DiscordBotOptions> options)
        {
            _options = options.Value;
            _client = client;
        }

        public async Task SendFile(byte[] file, string fileName, string text, GuildThread thread)
        {
            var channels = GetChannels(thread);

            for (int i = 0; i < channels.Length; i++)
                await channels[i].SendFileAsync(new MemoryStream(file), fileName, text);
        }

        public async Task SendMessage(string message, GuildThread thread, params ulong[] mentions)
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

            var channels = GetChannels(thread);
            var resultedMessage = messageBuilder.ToString();

            for (int i = 0; i < channels.Length; i++)
                await channels[i].SendMessageAsync(resultedMessage);
        }

        public async Task<IButtonsController> SendVotingButtons(string message, (string Name, string Id, BotButtonStyle Style)[] buttons, GuildThread thread, params ulong[] mentions)
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

            var channels = GetChannels(thread);
            var resultedMessage = messageBuilder.ToString();
            var builder = new ComponentBuilder();

            for (int i = 0; i < buttons.Length; i++)
            {
                var item = buttons[i];
                builder.WithButton(item.Name, item.Id, style: ConvertStyle(item.Style));
            }

            var restMessages = new List<RestUserMessage>(channels.Length);

            for (int i = 0; i < channels.Length; i++)
                restMessages.Add(await channels[i].SendMessageAsync(resultedMessage, components: builder.Build()));

            return new DiscordButtonsController(Interlocked.Increment(ref _idsCounter), restMessages.ToArray());
        }

        private ButtonStyle ConvertStyle(BotButtonStyle style)
        {
            switch (style)
            {
                case BotButtonStyle.Primary: return ButtonStyle.Primary;
                case BotButtonStyle.Secondary: return ButtonStyle.Secondary;
                case BotButtonStyle.Success: return ButtonStyle.Success;
                case BotButtonStyle.Danger: return ButtonStyle.Danger;
                case BotButtonStyle.Link: return ButtonStyle.Link;
                default: return ButtonStyle.Secondary;
            }
        }

        private SocketTextChannel[] GetChannels(GuildThread thread)
        {
            if (!_channels.TryGetValue(thread, out var channels))
            {
                var main = _client.GetGuild(_options.MainGuildId);

                var channelsList = new List<SocketTextChannel>();

                if (thread.HasFlag(GuildThread.EventsTape))
                    channelsList.Add(main.GetTextChannel(_options.EventsThreadId));
                if (thread.HasFlag(GuildThread.History))
                    channelsList.Add(main.GetTextChannel(_options.HistoryThreadId));
                if (thread.HasFlag(GuildThread.Leaderboard))
                    channelsList.Add(main.GetTextChannel(_options.LeaderboardThreadId));
                if (thread.HasFlag(GuildThread.Logging))
                    channelsList.Add(main.GetTextChannel(_options.LoggingThreadId));
                if (thread.HasFlag(GuildThread.TournamentChat))
                    channelsList.Add(main.GetTextChannel(_options.TournamentThreadId));

                channels = _channels[thread] = channelsList.ToArray();
            }

            return channels;
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

            public Task DisableButtons(string resultMessage)
            {
                return Task.WhenAll(_messages.Select(x => x.ModifyAsync(m =>
                {
                    m.Content = resultMessage;
                    m.Components = null;
                })));
            }
        }
    }
}
