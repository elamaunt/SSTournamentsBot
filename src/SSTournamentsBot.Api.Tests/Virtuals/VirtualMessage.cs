using SSTournaments;

namespace SSTournamentsBot.Api.Tests.Virtuals
{
    public class VirtualMessage
    {
        public SecondaryDomain.GuildThread Thread { get; set; }
        public ulong[] Mentions { get; set; }
        public string Message { get; set; }
        public byte[] File { get; set; }
        public string FileName { get; set; }

        public VirtualMessage(SecondaryDomain.GuildThread thread, ulong[] mentions)
        {
            Thread = thread;
            Mentions = mentions;
        }

        public VirtualMessage(SecondaryDomain.GuildThread thread, string message)
        {
            Thread = thread;
            Message = message;
        }

        public VirtualMessage(string message, ulong[] mentions, SecondaryDomain.GuildThread thread)
        {
            Message = message;
            Mentions = mentions;
            Thread = thread;
        }

        public VirtualMessage(byte[] file, string fileName, string text, SecondaryDomain.GuildThread thread)
        {
            File = file;
            FileName = fileName;
            Message = text;
            Thread = thread;
        }
    }
}