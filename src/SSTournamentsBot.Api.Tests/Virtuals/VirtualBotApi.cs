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

        public Task<string> GetMention(Context context, ulong id)
        {
            return Task.FromResult(id.ToString());
        }

        public Task<string> GetMentionForWaitingRole(Context context)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetUserName(Context context, ulong id)
        {
            return Task.FromResult("Bot" + id);
        }

        public Task Mention(Context context, SecondaryDomain.GuildThread thread, params ulong[] mentions)
        {
            Messages.Add(new VirtualMessage(thread, mentions));
            return Task.CompletedTask;
        }

        public Task MentionWaitingRole(Context context, SecondaryDomain.GuildThread thread)
        {
            return Task.CompletedTask;
        }

        public Task ModifyLastMessage(Context context, IText message, SecondaryDomain.GuildThread thread)
        {
            var last = Messages.FindLast(x => x.Thread == thread);

            if (last == null)
                Messages.Add(new VirtualMessage(thread, message.Build()));
            else
                last.Message = message.Build();
            return Task.CompletedTask;
        }

        public Task SendFile(Context context, byte[] file, string fileName, IText text, SecondaryDomain.GuildThread thread)
        {
            Messages.Add(new VirtualMessage(file, fileName, text.Build(), thread));
            return Task.CompletedTask;
        }

        public Task SendMessage(Context context, IText message, SecondaryDomain.GuildThread thread, params ulong[] mentions)
        {
            Messages.Add(new VirtualMessage(message.Build(), mentions, thread));
            return Task.CompletedTask;
        }

        public Task SendMessageToUser(Context context, IText message, ulong id)
        {
            return Task.CompletedTask;
        }

        public Task<IButtonsController> SendVotingButtons(Context context, IText message, SecondaryDomain.VotingOption[] buttons, SecondaryDomain.GuildThread thread, params ulong[] mentions)
        {
            return Task.FromResult((IButtonsController)new ButtonsControllerMock());
        }

        public Task<bool> ToggleWaitingRole(Context context, ulong id, bool? toValue)
        {
            return Task.FromResult(toValue ?? true);
        }
    }
}
