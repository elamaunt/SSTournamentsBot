using SSTournamentsBot.Api.Services;

namespace SSTournamentsBot.Api.Tests.Mocks
{
    public class GamesScannerMock : IGameScanner
    {
        public bool Active { get; set; }
        public SSTournaments.Domain.GameType GameTypeFilter { get; set; }
    }
}
