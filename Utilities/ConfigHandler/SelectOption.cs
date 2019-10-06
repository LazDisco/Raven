using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using Raven.Database;

namespace Raven.Utilities.ConfigHandler
{
    public static partial class ConfigHandler
    {
        /// <summary>Process the actual options</summary>
        public static Task<RestUserMessage> SelectOption(RavenGuild guild, ulong userId, SocketTextChannel channel, string[] args)
        {
            if (!uint.TryParse(args[0], out uint temp))
                return InvalidOption(channel);

            MessageBox option = (MessageBox)temp;

            switch (guild.UserConfiguration[userId])
            {
                // Don't know how this would be possible, but better safe than sorry.
                case MessageBox.BaseMenu:
                    return SelectSubMenu(guild, userId, channel, option);

                // Level Settings Sub Menu
                case MessageBox.LevelSettings:
                    {
                        if ((int)option is 7) // We need to cast our submenus to a higher value otherwise they cause problems when selecting options
                            option = MessageBox.LsSettingSubmenu;
                        switch (option)
                        {
                            case MessageBox.LsSetMinXp:
                                return LsSetMinXp(guild, userId, channel, args);

                            case MessageBox.LsSetMaxXp:
                                return LsSetMaxXp(guild, userId, channel, args);

                            case MessageBox.LsSetXpTime:
                                return LsSetMinXpTime(guild, userId, channel, args);

                            case MessageBox.LsAddLevelBinding:
                                return LsAddLevelBinding(guild, userId, channel, args);

                            case MessageBox.LsRemoveLevelBinding:
                                return LsRemoveLevelBinding(guild, userId, channel, args);

                            case MessageBox.LsListLevelBindings:
                                return LsListLevelBindings(guild, userId, channel, args);

                            case MessageBox.LsSettingSubmenu:
                                return SelectSubMenu(guild, userId, channel, option);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                // Level Settings Sub-Sub Menu
                case MessageBox.LsSettingSubmenu:
                    {
                        switch (option)
                        {
                            case MessageBox.LsSettingDisabled:
                                return LsSetLevelState(guild, userId, channel, LevelSettings.Disabled);

                            case MessageBox.LsSettingGlobalLevel:
                                return LsSetLevelState(guild, userId, channel, LevelSettings.GlobalLeveling);

                            case MessageBox.LsSettingGuildLevel:
                                return LsSetLevelState(guild, userId, channel, LevelSettings.GuildLeveling);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                // Welcome Message Settings Sub Menu
                case MessageBox.WelcomeSettings:
                    {
                        switch (option)
                        {
                            case MessageBox.WelcomeToggle:
                                guild.GuildSettings.WelcomeMessage.Enabled = !guild.GuildSettings.WelcomeMessage.Enabled;
                                guild.UserConfiguration[userId] = MessageBox.WelcomeSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.WelcomeSettings);

                            case MessageBox.WelcomeChannel:
                                return WelcomeSetChannel(guild, userId, channel, args);

                            case MessageBox.WelcomeMessage:
                                return WelcomeSetMessage(guild, userId, channel, args);

                            case MessageBox.WelcomePreview:
                                return WelcomePreviewMessage(guild, userId, channel, args);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                // Goodbye Settings Submenu
                case MessageBox.GoodbyeSettings:
                    {
                        switch (option)
                        {
                            case MessageBox.GoodbyeToggle:
                                guild.GuildSettings.GoodbyeMessage.Enabled = !guild.GuildSettings.GoodbyeMessage.Enabled;
                                guild.UserConfiguration[userId] = MessageBox.GoodbyeSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.GoodbyeSettings);

                            case MessageBox.GoodbyeChannel:
                                return GoodbyeSetChannel(guild, userId, channel, args);

                            case MessageBox.GoodbyeMessage:
                                return GoodbyeSetMessage(guild, userId, channel, args);

                            case MessageBox.GoodbyePreview:
                                return GoodbyePreviewMessage(guild, userId, channel, args);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                // Welcome Message Settings Sub Menu
                case MessageBox.LoggingSettings:
                    {
                        switch (option)
                        {
                            case MessageBox.LoggingModuleEnabled:
                                guild.LoggingSettings.Enabled = !guild.LoggingSettings.Enabled;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingChannel:
                                return LoggingSetChannel(guild, userId, channel, args);

                            case MessageBox.LoggingEnableAll:
                                guild.LoggingSettings.Enabled = true;
                                guild.LoggingSettings.Join = true;
                                guild.LoggingSettings.Leave = true;
                                guild.LoggingSettings.Ban = true;
                                guild.LoggingSettings.Msg = true;
                                guild.LoggingSettings.User = true;
                                guild.LoggingSettings.VoiceChannel = true;
                                guild.LoggingSettings.Role = true;
                                guild.LoggingSettings.GuildUpdate = true;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingDisableAll:
                                guild.LoggingSettings.Enabled = false;
                                guild.LoggingSettings.Join = false;
                                guild.LoggingSettings.Leave = false;
                                guild.LoggingSettings.Ban = false;
                                guild.LoggingSettings.Msg = false;
                                guild.LoggingSettings.User = false;
                                guild.LoggingSettings.VoiceChannel = false;
                                guild.LoggingSettings.Role = false;
                                guild.LoggingSettings.GuildUpdate = false;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingJoin:
                                guild.LoggingSettings.Join = !guild.LoggingSettings.Join;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingLeave:
                                guild.LoggingSettings.Leave = !guild.LoggingSettings.Leave;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingBan:
                                guild.LoggingSettings.Ban = !guild.LoggingSettings.Ban;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingMsg:
                                guild.LoggingSettings.Msg = !guild.LoggingSettings.Msg;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingUser:
                                guild.LoggingSettings.User = !guild.LoggingSettings.User;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingRole:
                                guild.LoggingSettings.Role = !guild.LoggingSettings.Role;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingVc:
                                guild.LoggingSettings.VoiceChannel = !guild.LoggingSettings.VoiceChannel;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            case MessageBox.LoggingGuildUpdate:
                                guild.LoggingSettings.GuildUpdate = !guild.LoggingSettings.GuildUpdate;
                                guild.UserConfiguration[userId] = MessageBox.LoggingSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.LoggingSettings);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                // Generic Settings (prefix, blacklists, module control, and probably more in the future)
                case MessageBox.GeneralSettings:
                    {
                        switch (option)
                        {
                            case MessageBox.GeneralSetPrefix:
                                return GeneralSetPrefix(guild, userId, channel, args);

                            case MessageBox.GeneralToggleInviteBlocking:
                                guild.GuildSettings.AutoblockInviteLinks = !guild.GuildSettings.AutoblockInviteLinks;
                                guild.UserConfiguration[userId] = MessageBox.GeneralSettings;
                                guild.Save();
                                return SelectSubMenu(guild, userId, channel, MessageBox.GeneralSettings);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                // Tag Settings Sub Menu
                case MessageBox.TagSettings:
                {
                    switch (option)
                    {
                        case MessageBox.TagAdd:
                            return AddTag(guild, userId, channel, args);

                        case MessageBox.TagRemove:
                            return RemoveTag(guild, userId, channel, args);

                        default:
                            return InvalidOption(channel);
                    }
                }

                case MessageBox.BlacklistSettings:
                    {
                        switch ((int)option)
                        {
                            case 1:
                                option = MessageBox.ConfigureDisallowedModules;
                                break;
                            case 2:
                                option = MessageBox.ConfigureDisallowedCommands;
                                break;
                            case 3:
                                option = MessageBox.ConfigureBlacklistedChannels;
                                break;
                            case 4:
                                option = MessageBox.ConfigureBlacklistedRoles;
                                break;
                            case 5:
                                option = MessageBox.ConfigureBlacklistedUsers;
                                break;
                        }

                        switch (option)
                        {
                            case MessageBox.ConfigureDisallowedModules:
                            case MessageBox.ConfigureDisallowedCommands:
                            case MessageBox.ConfigureBlacklistedChannels:
                            case MessageBox.ConfigureBlacklistedRoles:
                            case MessageBox.ConfigureBlacklistedUsers:
                                return SelectSubMenu(guild, userId, channel, option);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                case MessageBox.ConfigureDisallowedModules:
                    {
                        switch (option)
                        {
                            case MessageBox.BlacklistAddTo:
                                return BlacklistAddModule(guild, userId, channel, args);

                            case MessageBox.BlacklistRemoveFrom:
                                return BlacklistRemoveModule(guild, userId, channel, args);

                            case MessageBox.BlacklistDisplay:
                                return BlacklistDisplayModules(guild, channel);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                case MessageBox.ConfigureDisallowedCommands:
                    {
                        switch (option)
                        {
                            case MessageBox.BlacklistAddTo:
                                return BlacklistAddCommand(guild, userId, channel, args);

                            case MessageBox.BlacklistRemoveFrom:
                                return BlacklistRemoveCommand(guild, userId, channel, args);

                            case MessageBox.BlacklistDisplay:
                                return BlacklistDisplayCommands(guild, channel);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                case MessageBox.ConfigureBlacklistedChannels:
                    {
                        switch (option)
                        {
                            case MessageBox.BlacklistAddTo:
                                return BlacklistAddChannel(guild, userId, channel, args);

                            case MessageBox.BlacklistRemoveFrom:
                                return BlacklistRemoveChannel(guild, userId, channel, args);

                            case MessageBox.BlacklistDisplay:
                                return BlacklistDisplayChannels(guild, userId, channel, args);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                case MessageBox.ConfigureBlacklistedRoles:
                    {
                        switch (option)
                        {
                            case MessageBox.BlacklistAddTo:
                                return BlacklistAddRole(guild, userId, channel, args);

                            case MessageBox.BlacklistRemoveFrom:
                                return BlacklistRemoveRole(guild, userId, channel, args);

                            case MessageBox.BlacklistDisplay:
                                return BlacklistDisplayRoles(guild, userId, channel, args);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                case MessageBox.ConfigureBlacklistedUsers:
                    {
                        switch (option)
                        {
                            case MessageBox.BlacklistAddTo:
                                return BlacklistAddUser(guild, userId, channel, args);

                            case MessageBox.BlacklistRemoveFrom:
                                return BlacklistRemoveUser(guild, userId, channel, args);

                            case MessageBox.BlacklistDisplay:
                                return BlacklistDisplayUsers(guild, userId, channel, args);

                            default:
                                return InvalidOption(channel);
                        }
                    }

                default:
                    guild.UserConfiguration.Remove(userId);
                    guild.Save();
                    return channel.SendMessageAsync("I don't know how you got here, but I am gonna exit the menu just to be safe.\n" +
                                                    "(You should probably report this if it's reproducible.)");
            }
        }
    }
}
