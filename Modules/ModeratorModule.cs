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
    }
}
