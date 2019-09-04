using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord.WebSocket;
using Raven;
using Raven.Preconditions;
using Raven.Utilities;

namespace Raven.Modules
{
    [CheckBlacklistedModule]
    [Name("Help")]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _service;
        private readonly DiscordShardedClient _client;

        public HelpModule(DiscordShardedClient client, CommandService service)
        {
            _service = service;
            _client = client;
        }

        [CheckBlacklistedCommand]
        [Command("help"), Alias("?", "h")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };
            
            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                    {
                        description += $"{GlobalConfig.Prefix}{cmd.Aliases.First()}";
                        foreach (ParameterInfo param in cmd.Parameters)
                            description += " " + param.Name;
                        description += "\n";
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    EmbedFieldBuilder field = new EmbedFieldBuilder
                    {
                        Name = Utils.SplitPascalCase(module.Name),
                        Value = description,
                        IsInline = true
                    };
                    var p = GlobalConfig.PluginInfo.FirstOrDefault(x => x.ModuleNames.Any(y => y == module.Name));
                    if (p != null)
                        field.Name += $" ({p.PluginName})";

                    builder.AddField(field);
                }
            }

            int nearestNumber = Utils.GetNextHighestMulitple(builder.Fields.Count, 3);
            if (builder.Fields.Count - nearestNumber != 0)
            {
                for (int i = 0; i < nearestNumber - builder.Fields.Count; i++)
                    builder.AddField(Utils.NewEmptyField());
            }

            await ReplyAsync("", false, builder.Build());
        }

        [CheckBlacklistedCommand]
        [Command("help")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" + 
                              $"Summary: {cmd.Summary}";
                    x.IsInline = true;
                });
            }

            int nearestNumber = Utils.GetNextHighestMulitple(builder.Fields.Count, 3);
            if (builder.Fields.Count - nearestNumber != 0)
            {
                for (int i = 0; i < nearestNumber - builder.Fields.Count; i++)
                    builder.AddField(Utils.NewEmptyField());
            }
            await ReplyAsync("", false, builder.Build());
        }
    }
}
