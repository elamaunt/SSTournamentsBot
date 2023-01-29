using SSTournamentsBot.Api.Services;
using System.Collections.Generic;
using System.Linq;

namespace SSTournamentsBot.Api.Tests.Virtuals
{
    public class VirtualContextService : IContextService
    {
        public Context Context { get; set; }

        public IEnumerable<Context> AllContexts => Enumerable.Repeat(Context, 1);

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
