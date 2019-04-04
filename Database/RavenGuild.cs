using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Raven.Database
{
    public class RavenGuild
    {
        /// <summary>The internal guild Id from Discord.</summary>
        public ulong GuildId { get; }

        /// <summary>Name of the guild, mostly for logging purposes.</summary>
        public string Name { get; set; }

        /// <summary>The total amount of messages sent since the bot was added.</summary>
        public ulong TotalMessages { get; set; }

        /// <summary>The total amount of images sent since the bot was added. </summary>
        public uint TotalImages { get; set; }

        /// <summary>A map of users to menu items. This is how we calculate when people are using nested setup menus.</summary>
        public Dictionary<ulong, MessageBox> UserConfiguration { get; } = new Dictionary<ulong, MessageBox>();

        public RavenGuildCustomisations GuildSettings { get; set; }
        public RavenGuildLogging LoggingSettings { get; set; }

        public List<RavenPoll> Polls { get; set; }
        public List<RavenTag> Tags { get; set; }
        public List<RavenUser> Users { get; set; }

        public RavenStat MessageTracking { get; set; }
        public RavenStat ImageTracking { get; set; }
        public RavenStat UserTracking { get; set; }
    
        public RavenGuild(ulong guildId, string name)
        {
            GuildId = guildId;
            Name = name;
            TotalImages = 0;
            TotalMessages = 0;
            GuildSettings = new RavenGuildCustomisations();
            LoggingSettings = new RavenGuildLogging();

            Polls = new List<RavenPoll>();
            Tags = new List<RavenTag>();
            Users = new List<RavenUser>();

            MessageTracking = new RavenStat();
            ImageTracking = new RavenStat();
            UserTracking = new RavenStat();
        }

        // Methods

        public void Save()
        {
            RavenGuild old = RavenDb.GetGuild(GuildId);
            if (old != null)
                RavenDb.UpdateGuild(old, this);

            using (var session = RavenStore.Store.OpenSession())
            {
                session.Store(this, GuildId.ToString());
                session.SaveChanges();
            }
        }

        public RavenUser CreateNewUser(ulong id, string name, ushort discrim)
        {
            if (Users.FindIndex(x => x.UserId == id) != -1)
                return null;

            RavenUser user = new RavenUser(id, name, discrim);
            Users.Add(user);
            this.Save();
            return user;
        }

        public RavenUser GetUser(ulong id)
        {
            int index = Users.FindIndex(x => x.UserId == id);
            return index == -1 ? null : Users[index];
        }
    }

    public class RavenGuildLogging
    {
        /// <summary>If false all logging will be disabled.</summary>
        public bool Module { get; set; }
        /// <summary>The channel guildId that all logs will be sent to. </summary>
        public ulong? ChannelId { get; set; } // What channel are we logging to?

        // Discord Logging

        /// <summary>If true, users joining will be logged.</summary>
        public bool Join { get; set; }
        /// <summary>If true, users leaving will be logged.</summary>
        public bool Leave { get; set; }
        /// <summary>If true, users being banned will be logged.</summary>
        public bool Ban { get; set; }
        /// <summary>If true, edited/deleted messages will be logged</summary>
        public bool Msg { get; set; }
        /// <summary>If true, users being updated (roles, name change, etc) will be logged.</summary>
        public bool User { get; set; }
        /// <summary>If true, role updating/creating/deleting will be logged.</summary>
        public bool Role { get; set; }
        /// <summary>If true, users entering/leaving voice channels will be logged.</summary>
        public bool VoiceChannel { get; set; }
        /// <summary>If true, guild changes will be logged.</summary>
        public bool GuildUpdate { get; set; }

        // Custom Logging

        /// <summary>If true, changes to the currency system/user currency updates will be logged.</summary>
        public bool Currency { get; set; }
        /// <summary>If true, level changes will be logged.</summary>
        public bool Levels { get; set; }

        public RavenGuildLogging()
        {
            Module = false;
            ChannelId = null;
            Join = false;
            Leave = false;
            Ban = false;
            Msg = false;
            User = false;
            Role = false;
            VoiceChannel = false;
            GuildUpdate = false;
            Currency = false;
            Levels = false;
        }
    }

    public class RavenGuildCustomisations
    {
        public string Prefix { get; set; }

        /// <summary>Allow a custom message to greet a user when they join the server</summary>
        public RavenGuildMessage WelcomeMessage { get; set; }

        /// <summary>Allow a custom message to be sent to the server when someone leaves/is kicked from the guild.</summary>
        public RavenGuildMessage GoodbyeMessage { get; set; }

        /// <summary>Allow a custom kick message to be used when someone is removed via the bot.</summary>
        public RavenGuildMessage CustomKickMessage { get; set; }

        /// <summary>A list of channels that the bot will ignore commands from.</summary>
        public List<ulong> BlacklistedChannels { get; set; }

        /// <summary>A list of roles that the bot will ignore commands from.</summary>
        public List<ulong> BlacklistedRoles { get; set; }

        /// <summary>A list of users that the bot will ignore commands from.</summary>
        public List<ulong> BlacklistedUsers { get; set; }

        /// <summary>If true the bot will attempt to auto delete invite links from people who lack the manage server permission.</summary>
        public bool AutoblockInviteLinks { get; set; } = false;

        /// <summary>The configuration for the level module</summary>
        public RavenGuildLevelConfig LevelConfig { get; set; }

        public Dictionary<AllowedModules, bool> AllowedModules { get; set; }
        public Dictionary<AllowedCommands, bool> AllowedCommands { get; set; }

        public RavenGuildCustomisations()
        {
            Prefix = GlobalConfig.Prefix ?? "|";
            WelcomeMessage = new RavenGuildMessage();
            GoodbyeMessage = new RavenGuildMessage();
            CustomKickMessage = new RavenGuildMessage();

            BlacklistedChannels = new List<ulong>();
            BlacklistedRoles = new List<ulong>();
            BlacklistedUsers = new List<ulong>();
            
            LevelConfig = new RavenGuildLevelConfig(RavenDb.GlobalLevelConfig);

            AllowedModules = new Dictionary<AllowedModules, bool>();
            AllowedCommands = new Dictionary<AllowedCommands, bool>();
        }
    }

    public class RavenGuildMessage
    {
        public bool Enabed { get; set; }
        public string Message { get; set; }
        public ulong? ChannelId { get; set; }

        public RavenGuildMessage()
        {
            Enabed = false;
            Message = null;
            ChannelId = null;
        }
    }

    public class RavenTag
    {
        /// <summary>The string the user types in to get the message</summary>
        public string Tag { get; set; }

        /// <summary>The original author of the tag</summary>
        public ulong AuthorId { get; set; }

        /// <summary>The last user to update the tag</summary>
        public ulong LastUpdatedBy { get; set; }

        /// <summary>The current message of the tag</summary>
        public string Message { get; set; }

        /// <summary>A list of role ids that force it so only people with one the roles may use the tag</summary>
        public List<ulong> RestrictedRoles { get; set; }

        /// <summary>A list of channel ids that specify it so the tag can only be used in that channel</summary>
        public List<ulong> RestrictedChannels { get; set; }
    }

    public class RavenPoll
    {
        /// <summary>The internal message guildId from Discord that the reaction is assigned to.</summary>
        public ulong MessageId { get; set; }

        /// <summary>The internal channel guildId from Discord that will be the location for the embed to be posted.</summary>
        public ulong ChannelId { get; set; }

        /// <summary>How long in minutes will the poll last</summary>
        public float Duration { get; set; }

        /// <summary>Whether the poll has been setup yet</summary>
        public bool Deployed { get; set; }

        /// <summary>The optional image that should be attatched to the poll</summary>
        public string ImageUrl { get; set; }

        /// <summary>The poll description.</summary>
        public string Description { get; set; }

        /// <summary>The collective list of upvotes for the poll</summary>
        
        public RavenReaction Upvotes { get; set; }
        /// <summary>The collective list of downvotes for the poll</summary>
        public RavenReaction Downvotes { get; set; }

        public class RavenReaction
        {
            /// <summary>The amount of reactions</summary>
            public int Votes { get; set; }
            /// <summary>The internal emote guildId from Discord.</summary>
            public ulong ReactionId { get; set; }
        }
    }

    public class RavenStat
    {
        public RavenStat()
        {
            Enabled = false;
            ChannelId = null;
        }

        public bool Enabled { get; set; }
        public ulong? ChannelId { get; set; }
    }

    public class RavenGuildLevelConfig
    {
        public RavenGuildLevelConfig(int minXpGenerated, int maxXpGenerated, uint secondsBetweenXpGiven)
        {
            MinXpGenerated = minXpGenerated;
            MaxXpGenerated = maxXpGenerated;
            SecondsBetweenXpGiven = secondsBetweenXpGiven;
        }

        public RavenGuildLevelConfig(RavenGuildLevelConfig raven)
        {
            MinXpGenerated = raven.MinXpGenerated;
            MaxXpGenerated = raven.MaxXpGenerated;
            SecondsBetweenXpGiven = raven.SecondsBetweenXpGiven;
        }

        /// <summary>The minimum amount of xp a message can generate.</summary>
        public int MinXpGenerated { get; set; }

        /// <summary>The minimum amount of xp a message can generate.</summary>
        public int MaxXpGenerated { get; set; }

        /// <summary>How long must a user wait before they can generate xp again?</summary>
        public uint SecondsBetweenXpGiven { get; set; }

        /// <summary>The current level setting.
        /// If disabled, all level functionality is disabled.
        /// If Global, users will generate xp on a bot level, but not store it for the guild.
        /// If Guild, users will have an independant level table for the guild.</summary>
        public LevelSettings LevelSettings { get; set; } = LevelSettings.Disabled;
    }

    public enum AllowedModules
    {
        // fill in later
    }

    public enum AllowedCommands
    {
        // fill in later
    }

    public enum LevelSettings
    {
        Disabled,
        GlobalLeveling,
        GuildLeveling
    }

    public enum MenuFiles
    {
        BaseMenu = 0,
        LevelSettings = 1,

        // Submenus
        LsSubSettings = 30
    }

    public enum MessageBox
    {
        /// <summary>This is the very base menu item. The root configuration menu.</summary>
        BaseMenu = 0,
        LevelSettings = 1,

        // LevelSettings Submenu
        LsSetMinXp = 1,
        LsSetMaxXp = 2,
        LsSetXpTime = 3,
        LsSettingSubmenu = 4,

        // Level Settings Sub-Submenu
        LsSettingDisabled = 1,
        LsSettingGlobalLevel = 2,
        LsSettingGuildLevel = 3
    }
}
