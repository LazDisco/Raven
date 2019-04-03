using System;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Raven.Services.Events;

namespace Raven.Services
{
    public class DiscordEventHandler
    {
        private readonly DiscordEvents _events;

        public DiscordEventHandler(DiscordShardedClient client, IServiceProvider service,
            CommandService commandService)
        {
            _events = new DiscordEvents(client, service, commandService);

            client.Log += Logger.OnLogAsync;
            client.ShardReady += _events.ShardReadyAsync;
            client.MessageReceived += _events.MessageReceivedAsync;
        }
    }
}
