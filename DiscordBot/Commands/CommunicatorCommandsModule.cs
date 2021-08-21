using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
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
        public async Task RegisterCommand(CommandContext ctx, [Description("Your gameservers custom ID")] string serverId, [Description("The game of your server.")] string gameIdentification)
        {
            await ctx.TriggerTypingAsync();

            _ = Task.Run(async () => {
                DiscordColor col = DiscordColor.Green;
                string message = "TODO";

                // TODO: do stuff here

                var msg = await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(col)
                    .WithTitle(message)
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

        [Command("list-games")]
        [Description("List all available games")]
        public async Task ListGamesCommand(CommandContext ctx)
        {

            List<string> allGames = ComManager.ListAllGames();

            string message = string.Empty;
            foreach(var game in allGames)
            {
                message += game + ", ";
            }
            if(message.Length > 1)
                message = message.Substring(0, message.Length - 2);

            var msg = await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithAuthor("All available games:")
                    .WithTitle(message)
                    .Build())
                .SendAsync(ctx.Channel);
        }

        [Command("set-hostname")]
        [Description("Sets the hostname / IP for clients to reach the bot.")]
        [RequireOwner, RequireDirectMessage]
        public async Task SetHostnameCommand(CommandContext ctx, string hostname)
        {

            ComManager.SetHostname(hostname);


            var msg = await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithTitle($"Hostname set to '{hostname}'!")
                    .Build())
                .SendAsync(ctx.Channel);
        }
    }
}
