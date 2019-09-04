using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Raven.Database;
using Raven.Preconditions;
using Raven.Services.Events;

namespace Raven.Modules
{
    [Name("Profile")]
    [CheckBlacklistedModule]
    public class ProfileModule : ModuleBase<ShardedCommandContext>
    {
        [RequireContext(ContextType.Guild)]
        [Command("profile"), Priority(1)]
        [CheckBlacklistedCommand]
        public async Task GetProfileAsync([Remainder]string target)
        {
            RavenGuild guild = RavenDb.GetGuild(Context.Guild.Id);
            Regex regex = new Regex(@"<@([0-9]*)>");
            Match match = regex.Match(target);
            if (match.Success)
            {
                // See if what they gave was a real person
                ulong.TryParse(match.Groups[1].Value, out ulong id);
                if (id is 0)
                {
                    await ReplyAsync("Invalid User Mentioned.");
                    return;
                }

                RavenUser mentionedUser = guild.GetUser(id);
                if (mentionedUser is null)
                {
                    await ReplyAsync("The user specified doesn't exist within the system.");
                    return;
                }

                // Gotem.
                await ReplyAsync(null, false, DiscordEvents.GetUserProfile(id, guild));
            }

            else
            {
                RavenUser user = guild.Users.FirstOrDefault(x => x.Username.StartsWith(target));
                if (user is null)
                {
                    await ReplyAsync("Couldn't find anyone who's name was at all like that. To be fair, it's not a very indepth search.");
                    return;
                }

                await ReplyAsync(null, false, DiscordEvents.GetUserProfile(user.UserId, guild));
            }
        }

        [RequireContext(ContextType.Guild)]
        [Command("profile"), Priority(0)]
        [CheckBlacklistedCommand]
        public async Task GetOwnProfileAsync() => await ReplyAsync(null, false, DiscordEvents.GetUserProfile(Context.User.Id, RavenDb.GetGuild(Context.Guild.Id)));
        
        // The DM profile
        [RequireContext(ContextType.DM, Group = "Context")]
        [RequireContext(ContextType.Group, Group = "Context")]
        [Command("profile")]
        [CheckBlacklistedCommand]
        public async Task GetProfileAsync() => await ReplyAsync(null, false, DiscordEvents.GetUserProfile(Context.User.Id, null));
    }
}
