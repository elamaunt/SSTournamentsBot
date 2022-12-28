using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Domain
{
    public interface IButtonsController
    {
        long Id { get; }
        Task DisableButtons(string resultMessage);
        Task<bool> ContainsMessageId(ulong id);
    }
}
