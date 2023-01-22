using SSTournamentsBot.Api.Services;

namespace SSTournamentsBot.Api.Tests.Virtuals
{
    public class VirtualContextService : IContextService
    {
        public Context Context { get; set; }

        public Context GetContext(string name)
        {
            return Context;
        }

        public (string, Context) GetLocaleAndContext(ulong channel)
        {
            return ("ru", Context);
        }

        public Context GetMainContext()
        {
            return Context;
        }
    }
}
