using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

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
        [RequireUserPermission(GuildPermission.KickMembers)]
        [RequireBotPermission(GuildPermission.KickMembers)]
        public async Task Kick([Remainder]SocketGuildUser user)
        {
            if (user.Hierarchy > _discord.GetGuild(user.Guild.Id).CurrentUser.Hierarchy)
            {
                await ReplyAsync("So yeah, that didn't work.");
                return;
            }
            await ReplyAsync($"cya {user.Mention} :wave:");
            await user.KickAsync();
            
        }
    }
}
