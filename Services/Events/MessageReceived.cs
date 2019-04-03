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
            if (!(s is SocketUserMessage msg)) return;
            if (msg.Author.Id == discord.CurrentUser.Id) return;

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

            var guild = RavenDb.GetGuild(context.Guild.Id) ?? RavenDb.CreateNewGuild(context.Guild.Id, context.Guild.Name);
            if (guild.GuildSettings.LevelConfig.LevelSettings != LevelSettings.Disabled)
            {
                var user = RavenDb.GetUser(context.User.Id) ?? RavenDb.CreateNewUser(context.User.Id,
                               context.User.Username, context.User.DiscriminatorValue);

                // User is ready for extra xp
                if (user.XpLastUpdated.AddSeconds(RavenDb.GlobalLevelConfig.SecondsBetweenXpGiven) < DateTime.UtcNow)
                {
                    user.XpLastUpdated = DateTime.UtcNow;
                    user.Xp = Convert.ToUInt64(new Random().Next(RavenDb.GlobalLevelConfig.MinXpGenerated,
                                  RavenDb.GlobalLevelConfig.MaxXpGenerated + 1)) + user.Xp;
                    if (user.Xp > user.RequiredXp)
                    {
                        user = PostLevelProcessing(user, out Embed embed);
                        // Don't send the global message, maybe a setting in the future?
                    }

                    user.Save();
                }

                if (guild.GuildSettings.LevelConfig.LevelSettings == LevelSettings.GuildLeveling)
                {
                    user = guild.GetUser(context.User.Id) ?? guild.CreateNewUser(context.User.Id,
                               context.User.Username, context.User.DiscriminatorValue);
                    if (user.XpLastUpdated.AddSeconds(guild.GuildSettings.LevelConfig.SecondsBetweenXpGiven) < DateTime.UtcNow)
                    {
                        user.XpLastUpdated = DateTime.UtcNow;
                        user.Xp = Convert.ToUInt64(new Random().Next(guild.GuildSettings.LevelConfig.MinXpGenerated,
                                      guild.GuildSettings.LevelConfig.MaxXpGenerated + 1)) + user.Xp;
                        if (user.Xp > user.RequiredXp)
                        {
                            SocketRole role = ((SocketGuildUser) msg.Author).Roles.FirstOrDefault(x => x.Color.ToString() != "#0");
                            Color? color = role?.Color;
                            user = PostLevelProcessing(user, out Embed embed, color);
                            await context.Channel.SendMessageAsync("", false, embed);
                        }

                        user.Save();
                    }
                }
            }

            if (msg.MentionedUsers.Any(x => discord.Shards.Any(y => y.CurrentUser == x)) || msg.Content == "prefix")
            {
                await context.Channel.SendMessageAsync("This guild's prefix is: " + guild.GuildSettings.Prefix);
                return;
            }

            if (msg.HasStringPrefix(guild.GuildSettings.Prefix, ref argPos))
            {
                var result = await commandService.ExecuteAsync(context, argPos, service);

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }

        internal RavenUser PostLevelProcessing(RavenUser user,  out Embed embed, Color? color = null, RavenGuild guild = null)
        {
            if (color == null)
                color = new Color(255, 204, 0);
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

            embed = new EmbedBuilder()
            {
                Title = "Level Up!",
                Color = color,
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
