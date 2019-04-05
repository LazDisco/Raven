using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Raven.Database;

namespace Raven.Services.Events
{
    public partial class DiscordEvents
    {
        /// <summary>Called when a user joins the server. </summary>
        internal async Task GuildUserJoinAsync(SocketGuildUser user)
        {
            // Add it to the global database if they don't exist
            if (RavenDb.GetUser(user.Id) is null)
                RavenDb.CreateNewUser(user.Id, user.Username, user.DiscriminatorValue);

            // Get the guild this user is in
            RavenGuild guild = RavenDb.GetGuild(user.Guild.Id) ?? RavenDb.CreateNewGuild(user.Guild.Id, user.Guild.Name);

            // Update the total amount of users
            guild.TotalUsers = (uint)user.Guild.Users.Count;

            // If they rejoined, we'll store their old name to log
            bool rejoined = false;
            string username = string.Empty;

            // Get the user from that guild
            RavenUser guildUser = guild.GetUser(user.Id);
            if (guildUser is null) // if they don't exist, we'll need to create them
                guild.CreateNewUser(user.Id, user.Username, user.DiscriminatorValue);

            else
            {
                // They rejoined, update their name in case/discrim in case it changed
                rejoined = true;
                username = guildUser.Username + "#" + guildUser.Discriminator;
                guildUser.Username = user.Username;
                guildUser.Discriminator = user.DiscriminatorValue;
                guild.Save(); // Save the updated information, while storing the old one for logging purposes
            }

            // Process welcome message if one is set
            if (guild.GuildSettings.WelcomeMessage.Enabed)
            {
                // If the targeted channel is null or no longer exists or the message itself is undefined
                if (guild.GuildSettings.WelcomeMessage.ChannelId is null || user.Guild.GetTextChannel(guild.GuildSettings.WelcomeMessage.ChannelId.GetValueOrDefault()) is null
                    || string.IsNullOrWhiteSpace(guild.GuildSettings.WelcomeMessage.Message))
                {
                    // If the logging channel is setup, exists, and is enabled
                    if (!(guild.LoggingSettings.ChannelId is null) && !(user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.GetValueOrDefault()) is null)
                        && guild.LoggingSettings.Module)
                    {
                        // Log to the logging channel if it has been set
                        await user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value).SendMessageAsync(null, false, new EmbedBuilder()
                        {
                            Title = "Warning!",
                            Color = new Color(255, 128, 0), // Orange
                            Description = "Unable to send welcome message. Channel or message are currently null. Please reconfigure it.",
                            Footer = new EmbedFooterBuilder()
                            {
                                Text = $"{DateTime.UtcNow:ddd MMM d yyyy HH mm}"
                            }
                        }.Build());
                    }
                }

                else
                {
                    // If the logging channel is setup, exists, and is enabled
                    if (!(guild.LoggingSettings.ChannelId is null) && !(user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value) is null)
                        && guild.LoggingSettings.Module && guild.LoggingSettings.Join)
                    {
                        // Log a person has joined the server
                        string desc = $":package: **{user.Username}#{user.DiscriminatorValue}** has joined the server.";
                        if (rejoined) // if they rejoined, log that too
                            desc += $"\n This user has been in the server before. They used to be called: {username}";
                        await user.Guild.GetTextChannel(guild.LoggingSettings.ChannelId.Value).SendMessageAsync(null, false, new EmbedBuilder()
                        {
                            Description = desc,
                            Footer = new EmbedFooterBuilder()
                            {
                                IconUrl = user.GetAvatarUrl(),
                                Text = $"New User ({user.Guild.Users.Count}) | {DateTime.UtcNow:ddd MMM d yyyy HH mm}"
                            },
                            Color = new Color(0, 230, 0)
                        }.Build());
                    }

                    // Send the welcome message and repalce the server or user tags if they are present
                    await user.Guild.GetTextChannel(guild.GuildSettings.WelcomeMessage.ChannelId.Value)
                        .SendMessageAsync(guild.GuildSettings.WelcomeMessage.Message
                        .Replace("%SERVER%", user.Guild.Name)
                        .Replace("%USER%", user.Username));
                }
            }
        }
    }
}
