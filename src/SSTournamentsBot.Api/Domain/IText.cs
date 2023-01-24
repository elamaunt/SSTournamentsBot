using System.Globalization;

namespace SSTournamentsBot.Api.Domain
{
    public interface IText
    {
        string Build(CultureInfo culture = null);
    }
}