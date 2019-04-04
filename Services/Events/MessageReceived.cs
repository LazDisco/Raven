using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Database;

namespace Raven.Services.Events
{
    public partial class DiscordEvents
    {
        internal async Task MessageReceivedAsync(SocketMessage s)
        {
            if (!(s is SocketUserMessage msg)) return; // If this is not a message (could be a TTS, Image, File, etc)
            if (msg.Author.IsBot) return; // Ignore messages from bot users, which includes the bot itself.

            int argPos = 0;
            ShardedCommandContext context = new ShardedCommandContext(discord, msg);

            // If DM Channel, ignore all database based things.
            if (msg.Channel is IDMChannel || msg.Channel is IGroupChannel)
            {
                if (msg.HasStringPrefix(GlobalConfig.Prefix, ref argPos))
                {
                    var result = await commandService.ExecuteAsync(context, argPos, service);

                    if (!result.IsSuccess)     // If not successful, reply with the error.
                        await context.Channel.SendMessageAsync(result.ToString());
                    return;
                }
            }

            // Get the active database information for the current guild, or create it if it doesn't exist (for some reason)
            var guild = RavenDb.GetGuild(context.Guild.Id) ?? RavenDb.CreateNewGuild(context.Guild.Id, context.Guild.Name);

            // If the level settings are not disabled, we want to do our level processing. 
            if (guild.GuildSettings.LevelConfig.LevelSettings != LevelSettings.Disabled)
            {
                // Get the global database entry for the user, or create it if it doesn't exist.
                var user = RavenDb.GetUser(context.User.Id) ?? RavenDb.CreateNewUser(context.User.Id,
                               context.User.Username, context.User.DiscriminatorValue);

                // Is the user ready for extra XP?
                if (user.XpLastUpdated.AddSeconds(RavenDb.GlobalLevelConfig.SecondsBetweenXpGiven) < DateTime.UtcNow)
                {
                    user.XpLastUpdated = DateTime.UtcNow; // We are giving them XP so let's update the time stamp
                    user.Xp = Convert.ToUInt64(new Random().Next(RavenDb.GlobalLevelConfig.MinXpGenerated,
                                  RavenDb.GlobalLevelConfig.MaxXpGenerated + 1)) + user.Xp; // Generate a value between our two clamps, a little RNG.
                    if (user.Xp > user.RequiredXp) // Are they ready for a level up?
                    {
                        user = PostLevelProcessing(user, out Embed embed); // Level them up
                        // Don't send the global message, maybe a setting in the future?
                    }
                    user.Save(); // Save the global user
                }

                // Are they allowing guild leveling?
                if (guild.GuildSettings.LevelConfig.LevelSettings == LevelSettings.GuildLeveling)
                {
                    user = guild.GetUser(context.User.Id) ?? guild.CreateNewUser(context.User.Id,
                               context.User.Username, context.User.DiscriminatorValue); // Get the user or create it none exists

                    // Check if they area ready for XP on a guild level
                    if (user.XpLastUpdated.AddSeconds(guild.GuildSettings.LevelConfig.SecondsBetweenXpGiven) < DateTime.UtcNow)
                    {
                        user.XpLastUpdated = DateTime.UtcNow; // They are so we update the timestamp
                        user.Xp = Convert.ToUInt64(new Random().Next(guild.GuildSettings.LevelConfig.MinXpGenerated,
                                      guild.GuildSettings.LevelConfig.MaxXpGenerated + 1)) + user.Xp; // Generate a value between our two clamps
                        if (user.Xp > user.RequiredXp) // If they are ready to level up
                        {
                            // Get the first role they are assigned that has a non-default colour
                            SocketRole role = ((SocketGuildUser) msg.Author).Roles.FirstOrDefault(x => x.Color.ToString() != "#0");
                            Color? color = role?.Color; // Get the colour from the role, or null if we didn't find a colour.
                            user = PostLevelProcessing(user, out Embed embed, color); // Pass it in to get the result
                            await context.Channel.SendMessageAsync("", false, embed); // Post it
                        }

                        user.Save(); // Save the db entry
                    }
                }
            }

            // If the mention the bot directly, tell them the prefix. If they type just the word prefix, tell them.
            if (msg.MentionedUsers.Any(x => discord.Shards.Any(y => y.CurrentUser == x)) || msg.Content == "prefix")
            {
                await context.Channel.SendMessageAsync("This guild's prefix is: " + guild.GuildSettings.Prefix);
                return;
            }

            // Are they currently in a menu
            if (guild.UserConfiguration.ContainsKey(context.User.Id))
            {
                // They want off this wild ride
                if (msg.Content == "exit" || msg.Content.StartsWith("exit"))
                {
                    guild.UserConfiguration.Remove(context.User.Id); // Remove their menu entry
                    guild.Save(); // Save
                    await context.Channel.SendMessageAsync("Exited out of menu."); // Goodbye user
                    return;
                }

                // Otherwise we see if they specified a valid option
                else if (int.TryParse(msg.Content, out int option))
                {
                    // Handle it a bit differently if it's a different route
                    if (guild.UserConfiguration[context.User.Id] is MessageBox.BaseMenu)
                    {
                        // Normally we wouldn't use IsDefined due to it's lack of scalability,
                        // But for the root menu it actually scales rather well. Just need to watch out for the 2000 character limit.
                        if (Enum.IsDefined(typeof(MessageBox), option))
                        {
                            // Literally makes no difference in preformance, just trying to keep this file clean.
                            // Using this method, they can technically, if they know the submenu values,
                            // skip the parent menus and go straight to the sub menus. I don't really see this as an issue, to be honest.
                            await Utils.SelectSubMenu(guild, context.User.Id, context.Channel, (MessageBox) option);
                            return;
                        }

                        else // They didn't so lets give them another chance
                        {
                            await context.Channel.SendMessageAsync("The option you specified doesn't exist. The option should be"
                                + " just the number of the option you are trying to pick. Try again.");
                            return;
                        }
                    }

                    else
                    {
                        await Utils.SelectOption(guild, context.User.Id, context.Channel, msg.Content.Split(' '));
                    }
                }
            }

            // Ignore string prefixes if the person was currently in a menu
            else if (msg.HasStringPrefix(guild.GuildSettings.Prefix, ref argPos))
            {
                var result = await commandService.ExecuteAsync(context, argPos, service);

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }

        internal RavenUser PostLevelProcessing(RavenUser user,  out Embed embed, Color? color = null, RavenGuild guild = null)
        {
            if (color == null) // If we got a null value cause they lacked a role with colour
                color = new Color(114, 137, 218); // We give them a default one (an almost cornflower blue)
            // Make the next amount of xp required 20% more than the previous level
            double percentageIncrease = user.RequiredXp * 1.2f;

            // Assign their previous required xp to the amount they just passed.
            user.PrevRequiredXp = user.RequiredXp;

            // Round to the nearest five to keep our percentages nice
            user.RequiredXp = Convert.ToUInt64(Math.Round(percentageIncrease + (double) user.RequiredXp / 5) * 5);
            user.Level++; // Increase their level by 1.

            // TODO: Setup global/guild ranks
            // Is this a guild level up or a global levelup
            if (guild != null)
            {

            }

            else
            {

            }

            // Create a new embed
            embed = new EmbedBuilder()
            {
                Title = "Level Up!", // They leveled up
                Color = color, // The colour we were provided or assigned ourselves
                Description = $"You've Leveled Up! Your new level is {user.Level} and you require an additional " +
                              $"{user.RequiredXp - user.PrevRequiredXp} XP to level up again.",
                Footer = new EmbedFooterBuilder()
                {
                    Text = $"Your current rank is: {user.Rank}"
                }
            }.Build();
            return user;
        }
    }
}
