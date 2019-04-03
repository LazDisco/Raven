using System;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Raven.Services.Events
{
    public partial class DiscordEvents
    {
        private readonly IServiceProvider service;
        private readonly CommandService commandService;
        private readonly DiscordShardedClient discord;

        public DiscordEvents(DiscordShardedClient discord, IServiceProvider service, CommandService commandService)
        {
            this.service = service;
            this.commandService = commandService;
            this.discord = discord;
        }
    }
}
