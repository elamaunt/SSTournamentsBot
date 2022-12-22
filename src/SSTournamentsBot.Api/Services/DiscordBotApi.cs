using Discord.WebSocket;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public class DiscordBotApi : IBotApi
    {
        readonly DiscordSocketClient _client;

        public DiscordBotApi(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task SendFile(byte[] file, string fileName, string text)
        {
            foreach (var item in _client.Guilds)
            {
                var mainTextChannel = item.TextChannels.FirstOrDefault(x => x.Name == "основной") ?? item.DefaultChannel;
                await mainTextChannel.SendFileAsync(new MemoryStream(file), fileName, text);
            }
        }

        public async Task SendMessage(string message, params ulong[] mentions)
        {
            var messageBuilder = new StringBuilder();

            foreach (var id in mentions)
            {
                var user = await _client.GetUserAsync(id);
                messageBuilder.Append(user.Mention);
                messageBuilder.Append(' ');
            }

            if (mentions.Length > 0)
                messageBuilder.Append("\n");

            messageBuilder.Append(message);

            foreach (var item in _client.Guilds)
            {
                var mainTextChannel = item.TextChannels.FirstOrDefault(x => x.Name == "основной") ?? item.DefaultChannel;
                await mainTextChannel.SendMessageAsync(messageBuilder.ToString());
            }
        }

        public void StartVoting(Voting voting)
        {
            // TODO
        }

        public void TryCompleteVoting()
        {
            // TODO
        }
    }
}
