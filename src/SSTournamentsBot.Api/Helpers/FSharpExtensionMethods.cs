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

        public static bool IsSome<T>(this FSharpOption<T> self)
        {
            return FSharpOption<T>.get_IsSome(self);
        }

        public static bool IsNone<T>(this FSharpOption<T> self)
        {
            return FSharpOption<T>.get_IsNone(self);
        }
    }
}
