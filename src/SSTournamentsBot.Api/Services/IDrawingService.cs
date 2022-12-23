using static SSTournaments.Domain;

namespace SSTournamentsBot.Api.Services
{
    public interface IDrawingService
    {
        byte[] DrawToImage((Stage, StageBlock[])[] stages);
    }
}