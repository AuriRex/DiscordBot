using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordBot.Commands.Core;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Serilog;

namespace DiscordBot.Commands.Application
{
    public class LavaLinkAppCommandsModule : ApplicationCommandModule
    {
        public LavaLinkCommandsCore LavaLinkCommandsCore { private get; set; }

        static readonly Regex URL_REGEX = new Regex("https?:\\/\\/[a-zA-Z.0-9_\\-\\+\\#\\:\\;\\*\\%\\&\\!\\?\\=\\/]+");

        [SlashCommand("play", "Play a YouTube video or a file in a discord channel!")]
        public async Task PlayCommand(InteractionContext ctx, [Option("SearchOrURL", "Search term or link to video / file."), RemainingText] string searchOrUrl)
        {
            if(string.IsNullOrWhiteSpace(searchOrUrl))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(Utilities.CreateErrorEmbed("Your search term or URL must not be empty!")));
                return;
            }

            await ctx.DeferAsync();

            await LavaLinkCommandsCore.PlayCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, searchOrUrl, callback: async (callbackArgs) => {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(callbackArgs.Embed.Build()));
            });
        }

        [SlashCommand("queue", "Show what's up next!")]
        public async Task QueueCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            LavaLinkCommandsCore.QueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, async (callbackArgs) => {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(callbackArgs.Embed.Build()));
            });
        }

        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Play this")]
        public async Task CtxPlayThis(ContextMenuContext ctx) {

            await ctx.DeferAsync();

            var msgContent = ctx.TargetMessage?.Content;

            if(string.IsNullOrWhiteSpace(msgContent))
            {
                if(ctx.TargetMessage.Embeds.Count > 0)
                {
                    var embed = ctx.TargetMessage.Embeds.First();

                    msgContent = embed?.Url?.ToString() ?? embed?.Author?.Url?.ToString() ?? string.Empty;
                }
            }

            var match = URL_REGEX.Match(msgContent);

            if (match.Success)
            {
                foreach(Group g in match.Groups)
                {
                    var matchedString = g.Value;

                    if(matchedString.EndsWith("."))
                    {
                        matchedString = matchedString.Substring(0, matchedString.Length - 1);
                    }


                    Log.Information($"[{nameof(CtxPlayThis)}] URL Regex matched: \"{matchedString}\"");
                    await LavaLinkCommandsCore.PlayCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, matchedString, callback: async (callbackArgs) => {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(callbackArgs.Embed.Build()));
                    });
                    //await ctx.DeleteResponseAsync();
                }
                
                return;
            }

            Log.Information($"[{nameof(CtxPlayThis)}] URL Regex match failed: \"{msgContent}\"");

            var failedEmbed = new DiscordEmbedBuilder();
            failedEmbed.Title = "Couldn't find a link to play in the selected message!";
            failedEmbed.Url = ctx.TargetMessage.JumpLink.ToString();
            failedEmbed.Color = DiscordColor.Red;

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(failedEmbed.Build()));

        }

    }
}
