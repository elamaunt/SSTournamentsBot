using SSTournamentsBot.Api.Domain;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IBotApi
    {
        Task Mention(GuildThread thread, params ulong[] mentions);
        Task SendMessage(string message, GuildThread thread, params ulong[] mentions);
        Task SendFile(byte[] file, string fileName, string text, GuildThread thread);
        Task<IButtonsController> SendVotingButtons(string message, VotingOption[] buttons, GuildThread thread, params ulong[] mentions);
        Task ModifyLastMessage(string message, GuildThread thread);
        Task<string> GetUserName(ulong id);
        Task<string> GetMention(ulong id);
    }
}