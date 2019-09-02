using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Raven.Services;
using Raven.Utilities;

namespace Raven.Database
{
    internal static class RavenDb
    {
        private static List<RavenGuild> Guilds { get; set; } = new List<RavenGuild>();
        private static List<RavenUser> Users { get; set; } = new List<RavenUser>();
        internal static readonly RavenGuildLevelConfig GlobalLevelConfig = 
            new RavenGuildLevelConfig(GlobalConfig.MinGlobalXpGeneration,
                GlobalConfig.MaxGlobalXpGeneration, GlobalConfig.MinTimeBetweenXpGeneration);

        internal static void SetGuilds(List<RavenGuild> guilds)
        {
            Guilds = guilds;
        }

        internal static void SetUsers(List<RavenUser> users)
        {
            Users = users;
        }

        internal static List<RavenUser> GetAllUsers()
        {
            return Users;
        }

        internal static RavenGuild GetGuild(ulong id)
        {
            return Guilds.FirstOrDefault(x => x.GuildId == id);
        }

        internal static void UpdateGuild(RavenGuild oldGuild, RavenGuild newGuild)
        {
            int index = Guilds.FindIndex(x => x == oldGuild);
            if (index != -1)
                Guilds[index] = newGuild;
            else
                Logger.Log($"Unable to update guild: {newGuild.Name ?? newGuild.GuildId.ToString()} ({newGuild.GuildId})",
                    "Raven.cs - UpdateGuild", LogSeverity.Warning, "Index not found.");
        }

        internal static RavenUser GetUser(ulong id)
        {
            return Users.FirstOrDefault(x => x.UserId == id);
        }

        internal static void UpdateUser(RavenUser oldUser, RavenUser newUser)
        {
            int index = Users.FindIndex(x => x == oldUser);
            if (index != -1)
                Users[index] = newUser;
            else
                Logger.Log($"Unable to update user: {(newUser.Username + "#" + newUser.Discriminator)} ({newUser.UserId})",
                    "Raven.cs - UpdateGuild", LogSeverity.Warning, "Index not found.");
        }

        /// <summary>Generate a new guild with the default settings and add it to the database.
        /// This should never be run if the guild already exists within the database. </summary>
        internal static RavenGuild CreateNewGuild(ulong id, string name)
        {
            if (Guilds.FindIndex(x => x.GuildId == id) != -1)
                return null;

            RavenGuild guild = new RavenGuild(id, name);
            guild.Save();
            Guilds.Add(guild);
            return guild;
        }

        /// <summary>Generate a new user with the default settings and add it to the database.
        /// This should never be run if the user already exists within the database. </summary>
        internal static RavenUser CreateNewUser(ulong id, string name, ushort discrim, string avatarUrl)
        {
            if (Users.FindIndex(x => x.UserId == id) != -1)
                return null;

            RavenUser user = new RavenUser(id, name, discrim, avatarUrl);
            user.Save();
            Users.Add(user);
            return user;
        }
    }
}
