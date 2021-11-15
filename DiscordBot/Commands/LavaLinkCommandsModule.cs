using DiscordBot.Commands.Core;
using DiscordBot.Events;
using DiscordBot.Extensions;
using DiscordBot.Managers;
using DiscordBot.Models.Configuration;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    [RequireGuild]
    public class LavaLinkCommandsModule : BaseCommandModule
    {
        public EqualizerManager EqualizerManager { private get; set; }
        public MusicQueueManager MusicQueueManager { private get; set; }
        public LavaLinkCommandsCore LavaLinkCommandsCore { private get; set; }

        public Config BotConfig { private get; set; }

        [Command("play")]
        [Description("Play a video from YouTube or from a direct video/audio source URL.")]
        public async Task Play(CommandContext ctx, [Description("YouTube search or URL")] [RemainingText] string search)
        {
            if (string.IsNullOrEmpty(search)) return;

            if(search.StartsWith('<') && search.EndsWith('>'))
            {
                search = search.Substring(1, search.Length - 2);
            }

            await (await LavaLinkCommandsCore.PlayCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, search)).RespondTo(ctx);
        }

        [Command("play")]
        [Priority(10)]
        // play with no arguments as alias for resume
        public async Task Play(CommandContext ctx)
        {
            await Resume(ctx);
        }


        [Command("last-song")]
        [Aliases("last")]
        public async Task LastSong(CommandContext ctx)
        {
            await (await LavaLinkCommandsCore.PlayLastTrackCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member)).RespondTo(ctx);
        }

        [Command("unstuck")]
        [Aliases("force-leave")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task Unstuck(CommandContext ctx)
        {
            if(!Core.LavaLinkCommandsCore.TryGetGuildConnection(ctx.Client, ctx.Guild, out var conn))
            {
                return;
            }

            if(conn.IsConnected)
            {
                await conn.DisconnectAsync();
            }
        }

        [Command("shuffle")]
        [Description("Randomizes the order of the songs in the Queue.")]
        public async Task Shuffle(CommandContext ctx)
        {
            await Task.Run(async () => {
                await LavaLinkCommandsCore.ShuffleQueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member).RespondTo(ctx);
            });
        }

        [Command("queue-mode")]
        [Aliases("qm", "queuemode")]
        [Description("Change the way the Queue behaves and in extension the way the next song is chosen.")]
        public async Task QueueMode(CommandContext ctx, string queueModeString)
        {
            await LavaLinkCommandsCore.QueueModeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, queueModeString).RespondWithReactionOrRestTo(ctx);
        }

        [Command("clear-queue")]
        [Aliases("clear")]
        [Description("Remove all songs from the Queue.")]
        public async Task ClearQueue(CommandContext ctx)
        {
            await LavaLinkCommandsCore.ClearQueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member).RespondTo(ctx);
        }

        [Command("queue")]
        [Aliases("q")]
        [Description("Display information about the Queue including the next* few songs.")]
        public async Task Queue(CommandContext ctx)
        {
            await Task.Run(async () => {
                await LavaLinkCommandsCore.QueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member).RespondTo(ctx);
            });
        }

        [Command("force-skip")]
        [Aliases("fs", "forceskip", "skip")]
        [Description("Skip to the next song.")]
        public async Task ForceSkip(CommandContext ctx)
        {
            await (await LavaLinkCommandsCore.ForceSkipCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member)).RespondTo(ctx);
        }

        [Command("volume")]
        [Description("Volume control, 0 equals muted, 100 is default and 1000 being earrape crunchy.")]
        public async Task Volume(CommandContext ctx, [Description("Volume between 0 and 1000")] int volume)
        {
            await (await LavaLinkCommandsCore.VolumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, volume)).RespondWithReactionOrRestTo(ctx);
        }

        [Command("volume")]
        public async Task Volume(CommandContext ctx)
        {
            await (await LavaLinkCommandsCore.VolumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, null)).RespondTo(ctx);
        }

        [Command("equalizer")]
        [Aliases("eq")]
        [Description("Change the Equalizer settings.")]
        public async Task Equalizer(CommandContext ctx)
        {
            await LavaLinkCommandsCore.EqualizerCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member).RespondTo(ctx);
        }

        [Command("equalizer")]
        public async Task Equalizer(CommandContext ctx, [Description("Preset profile name"), RemainingText] string profileName)
        {
            await (await LavaLinkCommandsCore.EqualizerApplyProfileCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, profileName)).RespondTo(ctx);
        }

        [Command("equalizer-reset")]
        [Aliases("eq-reset")]
        [Description("Reset the Equalizer back to default.")]
        public async Task EqualizerReset(CommandContext ctx)
        {
            await Equalizer(ctx, EqualizerManager.RESET_PROFILE);
        }

        [Command("now-playing")]
        [Aliases("np", "nowplaying")]
        [Description("Show the currently playing song and it's playback position / total length.")]
        public async Task NowPlaying(CommandContext ctx)
        {
            await LavaLinkCommandsCore.NowPlayingCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member).RespondTo(ctx);
        }

        [Command("pause")]
        [Description("Pause playback.")]
        public async Task Pause(CommandContext ctx)
        {
            await (await LavaLinkCommandsCore.PauseCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member)).RespondWithReactionOrRestTo(ctx);
        }

        [Command("resume")]
        [Description("Resume playback.")]
        public async Task Resume(CommandContext ctx)
        {
            await (await LavaLinkCommandsCore.ResumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member)).RespondWithReactionOrRestTo(ctx);
        }

        [Command("join")]
        [Description("Makes the bot join a specific voice channel")]
        public async Task Join(CommandContext ctx, DiscordChannel channel)
        {
            var responseWrapper = new CommandResponse.CommandResponseWrapper();
            var conn = await LavaLinkCommandsCore.ConnectToVoice(ctx.Client, ctx.Channel, responseWrapper);
            var response = responseWrapper.ResponseOrEmpty;

            if(response.IsEmptyResponse) return;

            if (conn == null)
            {
                await ctx.RespondAsync(response.GetMessageBuilder());
                return;
            }

            var emoji = Config.GetGuildEmojiOrFallback(ctx.Client, BotConfig.CustomReactionSettings.JoinCommandReactionId, "⤴️");

            await ctx.Message.CreateReactionAsync(emoji);
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            DiscordChannel channel = ctx?.Member?.VoiceState?.Channel;

            if(channel == null || channel.Type != ChannelType.Voice || (channel?.Guild != null && channel.Guild.IsUnavailable))
            {
                await ctx.RespondAsync(new DiscordMessageBuilder().AddEmbed(Utilities.CreateErrorEmbed("You aren't connected to a voice channel!")));
                return;
            }

            await Join(ctx, channel);
        }

        [Command("leave")]
        [Description("Makes the bot leave the current voice channel")]
        public async Task Leave(CommandContext ctx)
        {
            await (await LavaLinkCommandsCore.LeaveCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member)).RespondWithReactionOrRestTo(ctx);
        }
    }
}
