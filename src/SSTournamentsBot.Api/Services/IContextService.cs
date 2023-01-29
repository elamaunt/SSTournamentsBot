using System.Collections.Generic;

namespace SSTournamentsBot.Api.Services
{
    public interface IContextService
    {
        Context GetMainContext();
        (string, Context) GetLocaleAndContext(ulong channel);
        Context GetContext(string name);

        IEnumerable<Context> AllContexts { get; }
    }
}