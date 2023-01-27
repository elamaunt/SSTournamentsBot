using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SSTournamentsBot.Api.Domain
{
    public class CompoundText: IText
    {
        readonly List<IText> _parts = new List<IText>();

        public CompoundText AppendLine(IText text)
        {
            _parts.Add(text);
            return this;
        }

        public string Build(CultureInfo culture = null)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < _parts.Count; i++)
                builder.AppendLine(_parts[i].Build(culture));

            return builder.ToString();
        }

        public override string ToString()
        {
            return Build();
        }
    }
}
