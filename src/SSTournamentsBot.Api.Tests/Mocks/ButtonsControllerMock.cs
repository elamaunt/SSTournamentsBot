using SSTournamentsBot.Api.Domain;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Tests.Mocks
{
    public class ButtonsControllerMock : IButtonsController
    {
        public long Id => 0;

        public Task<bool> ContainsMessageId(ulong id)
        {
            return Task.FromResult(true);
        }

        public Task DisableButtons(Text resultMessage, Func<ulong, CultureInfo> cultureSelector)
        {
            return Task.CompletedTask;
        }
    }
}
