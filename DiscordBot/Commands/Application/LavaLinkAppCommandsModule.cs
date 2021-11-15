using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordBot.Commands.Core;
using DiscordBot.Managers;
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
        public async Task PlayCommand(InteractionContext ctx, [Option("SearchOrURL", "Search term or link to video / file."), RemainingText] string searchOrUrl = "")
        {
            if(string.IsNullOrWhiteSpace(searchOrUrl))
            {
                await ResumeCommand(ctx);
                return;
            }

            await ctx.DeferAsync();

            var response = await LavaLinkCommandsCore.PlayCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, searchOrUrl);

            if(response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("last-song", "Replay the last played song.")]
        public async Task PlayLastTrackCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = await LavaLinkCommandsCore.PlayLastTrackCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("volume", "Change the playback volume.")]
        public async Task VolumeCommand(InteractionContext ctx, [Option("volume", "with 0 equals muted, 100 is default and 1000 being earrape crunchy")] long volume = -1)
        {
            await ctx.DeferAsync();

            var response = await LavaLinkCommandsCore.VolumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, (volume == -1 ? null : (int?) volume));

            if(response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());

        }

        [SlashCommand("skip", "Skip to the next song.")]
        public async Task ForceSkipCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = await LavaLinkCommandsCore.ForceSkipCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("shuffle", "Randomizes the order of the songs in the Queue.")]
        public async Task ShuffleQueueCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = LavaLinkCommandsCore.ShuffleQueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("queue-mode", "Change the way the Queue behaves and in extension the way the next song is chosen.")]
        public async Task QueueModeCommand(InteractionContext ctx, [Option("mode", "The mode to switch to.")] MusicQueueManager.QueueMode queueMode = MusicQueueManager.QueueMode.Default)
        {
            await ctx.DeferAsync();

            var response = LavaLinkCommandsCore.QueueModeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, queueMode);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("clear", "Remove all songs from the Queue.")]
        public async Task ClearQueueCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = LavaLinkCommandsCore.ClearQueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("pause", "Pause playback.")]
        public async Task PauseCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = await LavaLinkCommandsCore.PauseCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("resume", "Resume playback.")]
        public async Task ResumeCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = await LavaLinkCommandsCore.ResumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("now-playing", "What's this jammer that's playing right now called again?")]
        public async Task NowPlayingCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = LavaLinkCommandsCore.NowPlayingCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
        }

        [SlashCommand("queue", "Show what's up next!")]
        public async Task QueueCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var response = LavaLinkCommandsCore.QueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
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
                    var response = await LavaLinkCommandsCore.PlayCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, matchedString);

                    if(response.IsEmptyResponse)
                    {
                        await ctx.DeleteResponseAsync();
                        return;
                    }

                    await ctx.EditResponseAsync(response.GetWebhookBuilder());
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
