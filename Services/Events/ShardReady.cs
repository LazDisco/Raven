using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Raven.Services.Events
{
    public partial class DiscordEvents
    {
        internal async Task ShardReadyAsync(DiscordSocketClient client)
        {
            await Task.Run((() => client.SetActivityAsync(new Game($"Shard: {client.ShardId}", ActivityType.Watching))));

            if (discord.Shards.All(x => x.ConnectionState == ConnectionState.Connected))
            {
                Logger.Log("All Shards Connected. Bot is ready.", "Discord", LogSeverity.Info);
            }
        }
    }
}
