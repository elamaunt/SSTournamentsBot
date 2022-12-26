using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IBotApi
    {
        Task SendMessage(string message, GuildThread thread, params ulong[] mentions);
        Task SendFile(byte[] file, string fileName, string text, GuildThread thread);

        void StartVoting(Voting voting, GuildThread thread);
        void TryCompleteVoting();
    }
}