using SSTournaments;
using SSTournamentsBot.Api.Domain;
using SSTournamentsBot.Api.Services;
using SSTournamentsBot.Api.Tests.Mocks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SSTournamentsBot.Api.Tests.Virtuals
{
    public class VirtualBotApi : IBotApi
    {
        public List<VirtualMessage> Messages { get; } = new List<VirtualMessage>();

        public Task<string> GetMention(ulong id)
        {
            return Task.FromResult(id.ToString());
        }

        public Task<string> GetMentionForWaitingRole()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetUserName(ulong id)
        {
            return Task.FromResult("Bot" + id);
        }

        public Task Mention(SecondaryDomain.GuildThread thread, params ulong[] mentions)
        {
            Messages.Add(new VirtualMessage(thread, mentions));
            return Task.CompletedTask;
        }

        public Task MentionWaitingRole(SecondaryDomain.GuildThread thread)
        {
            return Task.CompletedTask;
        }
        public Task<bool> ToggleWaitingRole(bool? toValue)
        {
            return Task.FromResult(toValue ?? true);
        }
        public Task ModifyLastMessage(string message, SecondaryDomain.GuildThread thread)
        {
            var last = Messages.FindLast(x => x.Thread == thread);

            if (last == null)
                Messages.Add(new VirtualMessage(thread, message));
            else
                last.Message = message;
            return Task.CompletedTask;
        }

        public Task SendFile(byte[] file, string fileName, string text, SecondaryDomain.GuildThread thread)
        {
            Messages.Add(new VirtualMessage(file, fileName, text, thread));
            return Task.CompletedTask;
        }

        public Task SendMessage(string message, SecondaryDomain.GuildThread thread, params ulong[] mentions)
        {
            Messages.Add(new VirtualMessage(message, mentions, thread));
            return Task.CompletedTask;
        }

        public Task SendMessageToUser(string message, ulong id)
        {
            return Task.CompletedTask;
        }

        public Task<IButtonsController> SendVotingButtons(string message, SecondaryDomain.VotingOption[] buttons, SecondaryDomain.GuildThread thread, params ulong[] mentions)
        {
            return Task.FromResult((IButtonsController)new ButtonsControllerMock());
        }
    }
}
