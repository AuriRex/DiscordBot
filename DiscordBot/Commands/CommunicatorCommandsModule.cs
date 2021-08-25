using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    [Group("coms")]
    [RequireUserPermissions(DSharpPlus.Permissions.Administrator)]
    public class CommunicatorCommandsModule : BaseCommandModule
    {
        public CommunicationsManager ComManager { private get; set; }

        [Command("register")]
        [Description("Register your gameserver with ID and Game")]
        [RequireDirectMessage, Priority(10)]
        public async Task RegisterCommand(CommandContext ctx, [Description("Your gameservers custom ID")] string serverId, [Description("The game of your server.")] string serviceIdentification, [Description("The discord text channel to bind to")] DiscordChannel channel)
        {
            await ctx.TriggerTypingAsync();

            if(channel.IsPrivate)
            {
                await ctx.RespondAsync("An error occured, DMs are not valid channels to bind to!");
                return;
            }

            _ = Task.Run(async () => {

                // TODO: do stuff here
                if (!ComManager.RegisterServer(serverId, serviceIdentification, channel.Guild.Id, channel.Id))
                {
                    await ctx.RespondAsync("Couldn't register.");
                    return;
                }

                var hostInfo = ComManager.GetHostInfo();

                var msg = await new DiscordMessageBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Green)
                        .WithTitle($"Server added to DB, make sure to add this to your servers Communicator config file:")
                        .AddField("Hostname", $"`{hostInfo.hostname}`")
                        .AddField("Port", $"`{hostInfo.port}`")
                        .Build())
                    .SendAsync(ctx.Channel);
            });
        }

        [Command("register")]
        [RequireDirectMessage, Priority(-1)]
        public async Task RegisterTooFewArgsCommand(CommandContext ctx, [RemainingText] string rest)
        {
            await ctx.RespondAsync($"Invalid number of arguments! Type '**{ctx.Prefix}help {ctx.Command.Parent.Name} {ctx.Command.Name}**' for more info.");
        }

        [Command("list-services")]
        [Description("List all available services")]
        public async Task ListServicesCommand(CommandContext ctx)
        {

            List<string> allServices = ComManager.ListAllRegisteredServices();

            string message = string.Join(",", allServices);

            var msg = await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithAuthor("All available services:")
                    .WithTitle(message)
                    .Build())
                .SendAsync(ctx.Channel);
        }

        [Command("set-hostname")]
        [Description("Sets the hostname / IP for clients to reach the bot.")]
        [RequireOwner, RequireDirectMessage]
        public async Task SetHostnameCommand(CommandContext ctx, string hostname, int port)
        {
            ComManager.SetHostname(hostname, port);


            var msg = await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithTitle($"Hostname set to '{hostname}'!")
                    .Build())
                .SendAsync(ctx.Channel);
        }
    }
}
