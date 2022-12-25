using Microsoft.FSharp.Core;

namespace SSTournamentsBot.Api
{
    public static class FSharpExtensionMethods
    {
        public static T ValueOrDefault<T>(this FSharpOption<T> self)
        {
            if (FSharpOption<T>.get_IsSome(self))
                return self.Value;
            return default;
        }
    }
}
