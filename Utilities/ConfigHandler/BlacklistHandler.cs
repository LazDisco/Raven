using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using Raven.Database;

namespace Raven.Utilities.ConfigHandler
{
    public static partial class ConfigHandler
    {
        private static Task<RestUserMessage> BlacklistAddChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("ChannelName", typeof(string)));

            var tempChannel = channel.Guild.Channels.FirstOrDefault(x => x.Name == string.Join('-', args.Skip(1)).ToLower());
            tempChannel = tempChannel ?? channel.Guild.Channels.FirstOrDefault(x => x.Name.Contains(string.Join('-', args.Skip(1)).ToLower()));

            if (tempChannel is null) // If we found no matching channels
                return channel.SendMessageAsync("The specified channel could not be found. Please try again.");
            if (!(tempChannel is SocketTextChannel)) // If the channel found was not a valid text channel
                return channel.SendMessageAsync("The specified channel was not a text channel");

            guild.GuildSettings.BlacklistedChannels.Add(tempChannel.Id);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedChannels);
        }

        private static Task<RestUserMessage> BlacklistRemoveChannel(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("ChannelId", typeof(ulong)));

            if (!ulong.TryParse(args.ElementAtOrDefault(1), out ulong id))
                return channel.SendMessageAsync(ParamWrongFormat("ChannelId", typeof(ulong)));

            if (guild.GuildSettings.BlacklistedChannels.Any(x => x == id))
            {
                guild.GuildSettings.BlacklistedChannels.Remove(id);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedChannels);
            }

            else
                return channel.SendMessageAsync("The specified channel was not blacklisted.");
        }

        private static Task<RestUserMessage> BlacklistDisplayChannels(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (guild.GuildSettings.BlacklistedChannels.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted channels.");

            string channels = "Blacklisted Channels:\n";

            foreach (ulong i in guild.GuildSettings.BlacklistedChannels)
            {
                var tempChannel = channel.Guild.Channels.FirstOrDefault(x => x.Id == i);

                if (tempChannel is null)
                    channels += $"#INVALID-CHANNEL ({i})\n";
                else
                    channels += $"#{tempChannel.Name} ({i})\n";
            }

            if (channels.Length > 1900)
                return channel.SendMessageAsync("Too many channels to list. Cutting off after 1800 characters.\n"
                                                + channels.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + channels + "\n```");
        }

        private static Task<RestUserMessage> BlacklistAddRole(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a role name
                return channel.SendMessageAsync(GetMissingParam("RoleName", typeof(string)));

            var tempRole = channel.Guild.Roles.FirstOrDefault(x => x.Name == string.Join(' ', args.Skip(1)));
            tempRole = tempRole ?? channel.Guild.Roles.FirstOrDefault(x => x.Name.Contains(string.Join(' ', args.Skip(1))));

            if (tempRole is null) // If we found no matching channels
                return channel.SendMessageAsync("The specified role could not be found. Please try again.");

            guild.GuildSettings.BlacklistedRoles.Add(tempRole.Id);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedRoles);
        }

        private static Task<RestUserMessage> BlacklistRemoveRole(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null) // If they didn't provide a channel name
                return channel.SendMessageAsync(GetMissingParam("RoleId", typeof(ulong)));

            if (!ulong.TryParse(args.ElementAtOrDefault(1), out ulong id))
                return channel.SendMessageAsync(ParamWrongFormat("RoleId", typeof(ulong)));

            if (guild.GuildSettings.BlacklistedRoles.Any(x => x == id))
            {
                guild.GuildSettings.BlacklistedRoles.Remove(id);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedRoles);
            }

            else
                return channel.SendMessageAsync("The specified role was not blacklisted.");
        }

        private static Task<RestUserMessage> BlacklistDisplayRoles(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (guild.GuildSettings.BlacklistedRoles.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted roles.");

            string roles = "Blacklisted Roles:\n";

            foreach (ulong i in guild.GuildSettings.BlacklistedRoles)
            {
                var tempRole = channel.Guild.Roles.FirstOrDefault(x => x.Id == i);

                if (tempRole is null)
                    roles += $"DELETED-ROLE ({i})\n";
                else
                    roles += $"{tempRole.Name} ({i})\n";
            }

            if (roles.Length > 1900)
                return channel.SendMessageAsync("Too many roles to list. Cutting off after 1800 characters.\n"
                                                + roles.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + roles + "\n```");
        }

        private static Task<RestUserMessage> BlacklistAddUser(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("Username", typeof(string)));

            string name = string.Join(' ', args.Skip(1)).ToLower();

            var user = (channel.Guild.Users.FirstOrDefault(x => (x.Username + "#" + x.DiscriminatorValue).Contains(name)) ??
                        channel.Guild.Users.FirstOrDefault(x => x.Nickname.Contains(name))) ??
                        channel.Guild.Users.FirstOrDefault(x => x.Id.ToString() == name);

            if (user is null)
                return channel.SendMessageAsync("Could not find a matching user.");

            guild.GuildSettings.BlacklistedUsers.Add(user.Id);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedUsers);
        }

        private static Task<RestUserMessage> BlacklistRemoveUser(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("UserId", typeof(ulong)));

            if (!ulong.TryParse(args.ElementAtOrDefault(1), out ulong id))
                return channel.SendMessageAsync(ParamWrongFormat("UserId", typeof(ulong)));

            if (guild.GuildSettings.BlacklistedUsers.Any(x => x == id))
            {
                guild.GuildSettings.BlacklistedUsers.Remove(id);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureBlacklistedUsers);
            }

            else
                return channel.SendMessageAsync("The specified user was not blacklisted.");
        }

        private static Task<RestUserMessage> BlacklistDisplayUsers(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (guild.GuildSettings.BlacklistedUsers.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted users.");

            string users = "Blacklisted Users:\n";

            foreach (ulong i in guild.GuildSettings.BlacklistedUsers)
            {
                var user = channel.Guild.Users.FirstOrDefault(x => x.Id == i);
                if (user is null)
                {
                    RavenUser deletedRavenUser = guild.Users.FirstOrDefault(x => x.UserId == i);
                    string oldUsername = deletedRavenUser == null ? "Unknown" : deletedRavenUser.Username + "#" + deletedRavenUser.Discriminator;
                    users += $"USER-LEFT (ID: {i} - Old Name: {oldUsername})\n";
                }
                else
                    users += $"{user.Username} ({i})\n";
            }

            if (users.Length > 1900)
                return channel.SendMessageAsync("Too many users to list. Cutting off after 1800 characters.\n"
                                                + users.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + users + "\n```");
        }

        private static Task<RestUserMessage> BlacklistDisplayModules(RavenGuild guild, SocketTextChannel channel)
        {
            if (guild.GuildSettings.BlacklistedModules.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted modules.");

            string modules = "Blacklisted Modules:\n";

            foreach (string i in guild.GuildSettings.BlacklistedModules)
                modules += i + "\n";

            if (modules.Length > 1900)
                return channel.SendMessageAsync("Too many modules to list. Cutting off after 1800 characters.\n"
                                                + modules.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + modules + "\n```");
        }

        private static Task<RestUserMessage> BlacklistAddModule(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("ModuleName", typeof(string)));

            string name = string.Join(' ', args.Skip(1)).ToLower();

            guild.GuildSettings.BlacklistedModules.Add(name);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureDisallowedModules);
        }

        private static Task<RestUserMessage> BlacklistRemoveModule(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("ModuleName", typeof(string)));

            string name = string.Join(' ', args.Skip(1)).ToLower();

            if (guild.GuildSettings.BlacklistedModules.Any(x => x == name))
            {
                guild.GuildSettings.BlacklistedModules.Remove(name);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureDisallowedModules);
            }

            else
                return channel.SendMessageAsync("The specified module was not blacklisted.");
        }

        private static Task<RestUserMessage> BlacklistDisplayCommands(RavenGuild guild, SocketTextChannel channel)
        {
            if (guild.GuildSettings.BlacklistedModules.Count == 0)
                return channel.SendMessageAsync("There are currently no blacklisted commands.");

            string commands = "Blacklisted Commands:\n";

            foreach (string i in guild.GuildSettings.BlacklistedCommands)
                commands += i + "\n";

            if (commands.Length > 1900)
                return channel.SendMessageAsync("Too many commands to list. Cutting off after 1800 characters.\n"
                                                + commands.Substring(0, 1800));
            else
                return channel.SendMessageAsync("```cs\n" + commands + "\n```");
        }

        private static Task<RestUserMessage> BlacklistAddCommand(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("CommandName", typeof(string)));

            string name = string.Join(' ', args.Skip(1)).ToLower();

            guild.GuildSettings.BlacklistedCommands.Add(name);
            guild.Save();
            return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureDisallowedCommands);
        }

        private static Task<RestUserMessage> BlacklistRemoveCommand(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (args.ElementAtOrDefault(1) is null)
                return channel.SendMessageAsync(GetMissingParam("CommandName", typeof(string)));

            string name = string.Join(' ', args.Skip(1)).ToLower();

            if (guild.GuildSettings.BlacklistedCommands.Any(x => x == name))
            {
                guild.GuildSettings.BlacklistedCommands.Remove(name);
                guild.Save();
                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralConfigureDisallowedCommands);
            }

            else
                return channel.SendMessageAsync("The specified command was not blacklisted.");
        }
    }
}
