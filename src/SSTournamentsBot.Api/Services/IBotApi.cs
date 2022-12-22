using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IBotApi
    {
        Task SendMessage(string message, params ulong[] mentions);
        Task SendFile(byte[] file, string fileName, string text);

        void StartVoting(Voting voting);
        void TryCompleteVoting();
    }
}