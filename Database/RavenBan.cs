using System.Collections.Generic;

namespace Raven.Database
{
    public class RavenBanList
    {
        // The Id for Raven DB. We should only ever have a single ban list.
        public byte DefaultId { get; } = 0;
        public struct Ban
        {
            public ulong UserId { get; set; }
            /// <summary>The last known handle the ban-ee went by.</summary>
            public string UsernameDescrim { get; set; }
            public string Reason { get; set; }
            public string Evidence { get; set; }

            public ulong BannedById { get; set; }

            /// <summary>The last known handle the ban-er went by.</summary>
            public string BannedByUsernameDescrim { get; set; }
        }

        public List<ulong> UsersWithBanPermissions { get; set; }
        public List<Ban> BanList { get; set; }
    }
}
