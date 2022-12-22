using LiteDB;

namespace SSTournamentsBot.Api.Services
{
    public interface ILiteDbContext
    {
        LiteDatabase Database { get; }
    }
}