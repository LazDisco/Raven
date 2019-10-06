using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Raven.Database;
using Raven.Preconditions;

namespace Raven.Modules
{
    [Name("Moderator")]
    [RequireContext(ContextType.Guild)]
    [CheckBlacklistedModule]
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        private readonly DiscordShardedClient _discord;

        public ModeratorModule(DiscordShardedClient discord, CommandService service)
        {
            _service = service;
            _discord = discord;
        }

        [Command("kick")]
        [Summary("Kick the specified user.")]
        [RequireBotOwner(Group = "Permission")]
        [RequireUserPermission(GuildPermission.KickMembers, Group = "Permission")]
        [RequireBotPermission(GuildPermission.KickMembers)]
        [CheckBlacklistedCommand]
        public async Task Kick([Remainder]SocketGuildUser user)
        {
            if (user.Hierarchy > Context.Guild.CurrentUser.Hierarchy)
            {
                await ReplyAsync("I cannot kick someone who has a higher role that me. Caw.");
                return;
            }

            await ReplyAsync($"Bye, {user.Nickname ?? user.Username}. You probably wont be missed.");
            await user.KickAsync();
        }

        [Command("purge")]
        [Alias("prune")]
        [Summary("Delete the specified number of messages.")]
        [Remarks("Capped at 100 messages per purge. Mentioning people will delete only their messages. Does not delete messages older than 14 days.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [RequireBotPermission(GuildPermission.ManageMessages, Group = "Permission")]
        [RequireBotPermission(ChannelPermission.ManageMessages, Group = "Permission")]
        [CheckBlacklistedCommand]
        public async Task Purge(uint count, [Remainder]string args = "") // Dummy args param
        {
            if (count is 0)
            {
                await ReplyAsync("You have to delete a positive number of messages. Come on, you knew that.");
                return;
            }

            else if (count > 100)
                count = 100;

            if (Context.Message.MentionedUsers.Count is 0)
            {
                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, (int)count).FlattenAsync();
                if (messages.Count() is 0)
                {
                    await ReplyAsync("Channel is empty.");
                    return;
                }

                await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);
                await ReplyAsync("Done.");
            }

            else
            {
                IEnumerable<IMessage> messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, (int)count).FlattenAsync();
                IEnumerable<IMessage> filtered = messages.Where(x => Context.Message.MentionedUsers.Any(y => y.Id == x.Author.Id));
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(filtered);
                await ReplyAsync($"Searched the last {count} messages and deleted all from the mentioned users.");
            }
        }
    }
}
