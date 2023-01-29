namespace SSTournamentsBot.Api.Services
{
    public interface IGameScanner
    {
        void StartForContext(Context context);
        void StopForContext(Context context);
    }
}
