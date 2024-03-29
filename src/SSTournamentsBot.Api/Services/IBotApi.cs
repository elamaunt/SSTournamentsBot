﻿using SSTournamentsBot.Api.Domain;
using System.Threading.Tasks;
using static SSTournaments.SecondaryDomain;

namespace SSTournamentsBot.Api.Services
{
    public interface IBotApi
    {
        Task Mention(Context context, GuildThread thread, params ulong[] mentions);
        Task SendMessage(Context context, IText message, GuildThread thread, params ulong[] mentions);
        Task SendFile(Context context, byte[] file, string fileName, IText text, GuildThread thread);
        Task<IButtonsController> SendVotingButtons(Context context, IText message, VotingOption[] buttons, GuildThread thread, params ulong[] mentions);
        Task ModifyLastMessage(Context context, IText message, GuildThread thread);
        Task<string> GetUserName(Context context, ulong id);
        Task<string> GetMention(Context context, ulong id);
        Task SendMessageToUser(Context context, IText message, ulong id);
        Task<bool> ToggleWaitingRole(Context context, ulong id, bool? toValue);
        Task<string> GetMentionForWaitingRole(Context context);
        Task MentionWaitingRole(Context context, GuildThread thread);
        Task<bool> IsUserOnline(ulong id);
    }
}