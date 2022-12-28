using SSTournamentsBot.Api.Domain;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IBotApi
    {
        Task SendMessage(string message, GuildThread thread, params ulong[] mentions);
        Task SendFile(byte[] file, string fileName, string text, GuildThread thread);
        Task<IButtonsController> SendButtons(string message, (string Name, string Id, BotButtonStyle Style)[] buttons, GuildThread thread, params ulong[] mentions);
    }
}