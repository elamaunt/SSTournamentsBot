using System;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Domain
{
    public interface IButtonsController
    {
        long Id { get; }
        Task DisableButtons(Text resultMessage, Func<ulong, CultureInfo> cultureSelector);
        Task<bool> ContainsMessageId(ulong id);
    }
}
