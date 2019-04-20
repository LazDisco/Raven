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
            if (msg.Author.IsBot || msg.Author.IsWebhook) return; // Ignore messages from bot users, which includes the bot itself.

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

            if (!context.Guild.CurrentUser.GuildPermissions.Administrator && msg.HasStringPrefix(guild.GuildSettings.Prefix, ref argPos))
            {
                await context.Channel.SendMessageAsync("The bot is not currently set as an administrator." +
                    "Commands will be ignored until the bot is granted the Administrator permission.");
                return;
            }

            if (msg.Content.Contains("discord.gg/") && !((SocketGuildUser) context.User).GuildPermissions.ManageGuild
                                                    && guild.GuildSettings.AutoblockInviteLinks)
            {
                await msg.DeleteAsync();
                await context.Channel.SendMessageAsync("This server does not allow the posting of Discord server invites by non-moderators.");
                return;
            }

            // If the level settings are not disabled, we want to do our level processing. 
            if (guild.GuildSettings.LevelConfig.LevelSettings != LevelSettings.Disabled)
            {
                // Get the global database entry for the user, or create it if it doesn't exist.
                var user = RavenDb.GetUser(context.User.Id) ?? RavenDb.CreateNewUser(context.User.Id,
                               context.User.Username, context.User.DiscriminatorValue, context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl());

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
                    // Get the user or create them if they don't exist.
                    RavenUser guildUser = guild.GetUser(context.User.Id) ?? guild.CreateNewUser(context.User.Id,
                               context.User.Username, context.User.DiscriminatorValue, context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl());

                    if (guildUser.UserId == 0)
                    {
                        // This is weird unintentional behaviour, but sometimes it happens
                        // Need to investigate further
                        await context.Channel.SendMessageAsync("Your user ID was 0 for some reason. Please try again.");
                        return;
                    }

                    // Check if they area ready for XP on a guild level
                    if (guildUser.XpLastUpdated.AddSeconds(guild.GuildSettings.LevelConfig.SecondsBetweenXpGiven) < DateTime.UtcNow)
                    {
                        guildUser.XpLastUpdated = DateTime.UtcNow; // They are so we update the timestamp
                        guildUser.Xp = Convert.ToUInt64(new Random().Next(guild.GuildSettings.LevelConfig.MinXpGenerated,
                                      guild.GuildSettings.LevelConfig.MaxXpGenerated + 1)) + guildUser.Xp; // Generate a value between our two clamps
                        if (guildUser.Xp > guildUser.RequiredXp) // If they are ready to level up
                        {
                            // Get the first role they are assigned that has a non-default colour
                            SocketRole role = ((SocketGuildUser) msg.Author).Roles.FirstOrDefault(x => x.Color.ToString() != "#0");
                            Color? color = role?.Color; // Get the colour from the role, or null if we didn't find a colour.
                            guildUser = PostLevelProcessing(guildUser, out Embed embed, color); // Pass it in to get the result
                            await context.Channel.SendMessageAsync("", false, embed); // Post it
                        }

                        int index = guild.Users.FindIndex(x => x.UserId == context.User.Id);
                        if (index != -1) // I don't think this should ever happend, but better safe than sorry
                            guild.Users[index] = guildUser; // Update it 

                        guild.Save(); // Save the db entry
                    }
                }
            }

            // If the mention the bot directly, tell them the prefix. If they type just the word prefix, tell them.
            if ((msg.MentionedUsers.All(x => discord.Shards.Any(y => y.CurrentUser.Id == x.Id)) && msg.MentionedUsers.Count > 0) || msg.Content.ToLower() == "prefix")
            {
                await context.Channel.SendMessageAsync("This guild's prefix is: " + guild.GuildSettings.Prefix);
                return;
            }

            // If they mention themselves display their global user profile
            else if (msg.MentionedUsers.All(x => context.User.Id == x.Id) && msg.MentionedUsers.Count > 0)
            {
                Embed embed = GetUserProfile(context.User.Id, null);
                await context.Channel.SendMessageAsync(null, false, embed);
                return;
            }

            // Ignore string prefixes if the person was currently in a menu
            else if (msg.HasStringPrefix(guild.GuildSettings.Prefix, ref argPos))
            {
                if (!guild.UserConfiguration.ContainsKey(context.User.Id))
                {
                    var result = await commandService.ExecuteAsync(context, argPos, service);

                    if (!result.IsSuccess)
                        await context.Channel.SendMessageAsync(result.ToString());
                    return;
                }

                else
                {
                    await context.Channel.SendMessageAsync("You are currently in a menu. Respond to it or type 'exit' to leave it.");
                }
            }

            // Are they currently in a menu
            if (guild.UserConfiguration.ContainsKey(context.User.Id))
            {
                string[] args = msg.Content.Split(' ');
                // They want off this wild ride
                if (msg.Content == "exit" || msg.Content.StartsWith("exit"))
                {
                    guild.UserConfiguration.Remove(context.User.Id); // Remove their menu entry
                    guild.Save(); // Save
                    await context.Channel.SendMessageAsync("Exited out of menu."); // Goodbye user
                    return;
                }

                else if (msg.Content == "back" || msg.Content.StartsWith("back"))
                {
                    switch (guild.UserConfiguration[context.User.Id])
                    {
                        case MessageBox.BaseMenu:
                            guild.UserConfiguration.Remove(context.User.Id); // Remove their menu entry
                            guild.Save(); // Save
                            await context.Channel.SendMessageAsync("Exited out of menu."); // Goodbye user
                            return;
                        case MessageBox.LsSettingSubmenu:
                            guild.UserConfiguration[context.User.Id] = MessageBox.LevelSettings;
                            break;
                        // By default we assume they are one menu deep
                        default:
                            guild.UserConfiguration[context.User.Id] = MessageBox.BaseMenu;
                            break;
                    }
                    guild.Save();
                    await ConfigHandler.SelectSubMenu(guild, context.User.Id, context.Guild.GetTextChannel(context.Channel.Id), guild.UserConfiguration[context.User.Id]);
                    return;
                }

                // Otherwise we see if they specified a valid option
                else if (int.TryParse(args[0], out int option))
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
                            await ConfigHandler.SelectSubMenu(guild, context.User.Id, context.Guild.GetTextChannel(context.Channel.Id), (MessageBox)option);
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
                        await ConfigHandler.SelectOption(guild, context.User.Id, context.Guild.GetTextChannel(context.Channel.Id), args);
                    }
                }
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

        internal static Embed GetUserProfile(ulong id, RavenGuild guild)
        {
            // If it's a global user we want to fetch different data
            if (guild is null)
            {
                // Fairly certain we don't need to do null checking here
                RavenUser user = RavenDb.GetUser(id);
                EmbedBuilder embed = new EmbedBuilder
                {
                    Title = $"User Information for {user.Username}#{user.Discriminator}",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = $"Rank: {user.Rank}",
                        IconUrl = user.AvatarUrl
                    },
                    Color = Color.Blue,
                    ImageUrl = user.AvatarUrl
                };
                embed.WithCurrentTimestamp();

                // Get all the levels/XP values from the guild users
                List<Tuple<ushort, ulong>> levels = RavenDb.GetAllUsers().Select(x => Tuple.Create(x.Level, x.Xp)).ToList();
                levels.Sort(); // Sort them
                levels.Reverse(); // Reverse them so they go from highest to lowest

                List<Tuple<ushort, ulong>> peopleWithSameLevels = levels.Where(x => x.Item1 == user.Level).ToList();
                peopleWithSameLevels = peopleWithSameLevels.OrderBy(x => x.Item2).ToList();
                int offset = peopleWithSameLevels.FindIndex(x => x.Item2 == user.Xp);

                embed.AddField($"XP: {user.Xp} / {user.RequiredXp}\n"
                               + $"Level: {user.Level} ({Math.Floor(((decimal)user.Xp - user.PrevRequiredXp)/(user.RequiredXp - user.PrevRequiredXp) * 100)}%)\n"
                               + $"Rank: {user.Rank}\n"
                               + $"Leaderboard: {levels.FindIndex(x => x.Item1 == user.Level) + offset + 1} / {levels.Count}"
                               + $"First Seen: {user.JoinedDateTime:yyyy-MM-dd HH:mm}", "\u200B");
                return embed.Build();
            }

            else
            {
                RavenUser user = guild.GetUser(id);
                EmbedBuilder embed = new EmbedBuilder
                {
                    Title = $"User Information for {user.Username}#{user.Discriminator}",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = $"Rank: {user.Rank}",
                        IconUrl = user.AvatarUrl
                    },
                    Color = Color.Blue,
                    ImageUrl = user.AvatarUrl
                };
                embed.WithCurrentTimestamp();

                // Get all the levels/XP values from the guild users
                List<Tuple<ushort, ulong>> levels = guild.Users.Select(x => Tuple.Create(x.Level, x.Xp)).ToList();
                levels.Sort(); // Sort them
                levels.Reverse(); // Reverse them so they go from highest to lowest

                List<Tuple<ushort, ulong>> peopleWithSameLevels = levels.Where(x => x.Item1 == user.Level).ToList();
                peopleWithSameLevels = peopleWithSameLevels.OrderBy(x => x.Item2).ToList();
                int offset = peopleWithSameLevels.FindIndex(x => x.Item2 == user.Xp);

                embed.AddField($"XP: {user.Xp} / {user.RequiredXp}\n"
                               + $"Level: {user.Level} ({Math.Floor(((decimal)user.Xp - user.PrevRequiredXp) / (user.RequiredXp - user.PrevRequiredXp) * 100)}%)\n"
                               + $"Rank: {user.Rank}\n"
                               + $"Leaderboard: {levels.FindIndex(x => x.Item1 == user.Level) + offset + 1} / {levels.Count}\n"
                               + $"First Seen: {user.JoinedDateTime:yyyy-MM-dd HH:mm}", "\u200B");
                return embed.Build();
            }
        }
    }
}
