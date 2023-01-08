using Microsoft.Extensions.Options;

namespace SSTournamentsBot.Api.Tests.Virtuals
{
    public class VirtualOptions<T> : IOptions<T> where T : class, new()
    {
        public T Value { get; set; }

        public VirtualOptions()
        {
        }

        public VirtualOptions(T value)
        {
            Value = value;
        }
    }
}
