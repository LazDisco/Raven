using System;
using Newtonsoft.Json;

namespace Raven.Database
{
    public class RavenUser
    {
        /// <summary>The internal guild Id from Discord.</summary>
        [JsonIgnore]
        public ulong UserId { get; set; }

        /// <summary>The amount of xp this user has. This can be local to the guild or over all servers.</summary>
        public ulong Xp { get; set; }

        /// <summary>The amount of xp the user requires for their next level</summary>
        public ulong RequiredXp { get; set; }

        /// <summary>The amount of xp the user required for their last level.</summary>
        public ulong PrevRequiredXp { get; set; }

        /// <summary>The current level of this user. This can be local to the guild or over all servers.</summary>
        public ushort Level { get; set; }

        /// <summary>Certain levels can have a rank assigned to them, giving them a title.</summary>
        public string Rank { get; set; }

        /// <summary>Allow people to give certain users custom ranks that wont be replaced by the automatic guild rank system. </summary>
        public bool HasCustomRank { get; set; }

        /// <summary>The last known username of the user. Might be incorrect if they left a guild and changed it.</summary>
        public string Username { get; set; }

        /// <summary>The last known avatar they had</summary>
        public string AvatarUrl { get; set; }

        /// <summary>The last known Discriminator of the user. Might be incorrect if they left a guild and changed it. </summary>
        public ushort Discriminator { get; set; }

        /// <summary>The time at which their XP was last updated.</summary>
        public DateTime XpLastUpdated { get; set; }

        /// <summary>The time at which their XP was last updated.</summary>
        public DateTime JoinedDateTime { get; set; }

        public RavenUser(ulong id, string username, ushort discrim, string avatarUrl)
        {
            UserId = id;
            Xp = 0;
            RequiredXp = 50;
            PrevRequiredXp = 0;
            Level = 1;
            Rank = "None";
            AvatarUrl = avatarUrl;
            Username = username;
            Discriminator = discrim;
            HasCustomRank = false;
            XpLastUpdated = DateTime.UtcNow;
            JoinedDateTime = DateTime.UtcNow;
        }

        public void Save()
        {
            RavenUser old = RavenDb.GetUser(UserId);
            if (old != null)
                RavenDb.UpdateUser(old, this);

            using (var session = RavenStore.Store.OpenSession())
            {
                session.Store(this, UserId.ToString());
                session.SaveChanges();
            }
        }
    }
}
