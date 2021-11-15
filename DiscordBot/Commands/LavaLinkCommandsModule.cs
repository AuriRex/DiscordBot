using DiscordBot.Attributes;
using DiscordBot.Commands.Core;
using DiscordBot.Managers;
using DiscordBot.Models.Configuration;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var response = await LavaLinkCommandsCore.PlayCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, search);

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
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
            var response = await LavaLinkCommandsCore.PlayLastTrackCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
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
                var response = LavaLinkCommandsCore.ShuffleQueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

                if (response.IsEmptyResponse) return;

                await ctx.RespondAsync(response.GetMessageBuilder());
            });
        }

        [Command("queue-mode")]
        [Aliases("qm", "queuemode")]
        [Description("Change the way the Queue behaves and in extension the way the next song is chosen.")]
        public async Task QueueMode(CommandContext ctx, string queueModeString)
        {
            await Task.Run(async () => {
                var response = LavaLinkCommandsCore.QueueModeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, queueModeString);

                if (response.Reaction != null)
                {
                    await ctx.Message.CreateReactionAsync(response.Reaction);
                    return;
                }

                if (response.IsEmptyResponse) return;

                await ctx.RespondAsync(response.GetMessageBuilder());
            });
        }

        [Command("clear-queue")]
        [Aliases("clear")]
        [Description("Remove all songs from the Queue.")]
        public async Task ClearQueue(CommandContext ctx)
        {
            await Task.Run(async () => {
                var response = LavaLinkCommandsCore.ClearQueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

                if (response.IsEmptyResponse) return;

                await ctx.RespondAsync(response.GetMessageBuilder());
            });
        }

        [Command("queue")]
        [Aliases("q")]
        [Description("Display information about the Queue including the next* few songs.")]
        public async Task Queue(CommandContext ctx)
        {
            await Task.Run(async () => {
                var response = LavaLinkCommandsCore.QueueCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

                if (response.IsEmptyResponse) return;

                await ctx.RespondAsync(response.GetMessageBuilder());
            });
        }

        [Command("force-skip")]
        [Aliases("fs", "forceskip", "skip")]
        [Description("Skip to the next song.")]
        public async Task ForceSkip(CommandContext ctx)
        {
            var response = await LavaLinkCommandsCore.ForceSkipCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
        }

        [Command("volume")]
        [Description("Volume control, 0 equals muted, 100 is default and 1000 being earrape crunchy.")]
        public async Task Volume(CommandContext ctx, [Description("Volume between 0 and 1000")] int volume)
        {
            var response = await LavaLinkCommandsCore.VolumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, volume);

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
        }

        [Command("volume")]
        public async Task Volume(CommandContext ctx)
        {
            var response = await LavaLinkCommandsCore.VolumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member, null);

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
        }

        [Command("equalizer")]
        [Aliases("eq")]
        [Description("Change the Equalizer settings.")]
        public async Task Equalizer(CommandContext ctx)
        {
            var response = LavaLinkCommandsCore.EqualizerCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
        }

        [Command("equalizer")]
        public async Task Equalizer(CommandContext ctx, [Description("Preset profile name")] [RemainingText] string text)
        {
            // TODO: add presets
            if(text.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                await EqualizerReset(ctx);
                return;
            }

            await Equalizer(ctx);
        }

        [Command("equalizer-reset")]
        [Aliases("eq-reset")]
        [Description("Reset the Equalizer back to default.")]
        public async Task EqualizerReset(CommandContext ctx)
        {
            var conn = await GetGuildConnection(ctx);

            if (conn == null) return;

            await conn.ResetEqualizerAsync();

            await ctx.RespondAsync("Equalizer has been reset!");
        }

        [Command("now-playing")]
        [Aliases("np", "nowplaying")]
        [Description("Show the currently playing song and it's playback position / total length.")]
        public async Task NowPlaying(CommandContext ctx)
        {
            var response = LavaLinkCommandsCore.NowPlayingCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
        }

        [Command("pause")]
        [Description("Pause playback.")]
        public async Task Pause(CommandContext ctx)
        {
            var response = await LavaLinkCommandsCore.PauseCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.Reaction != null)
            {
                await ctx.Message.CreateReactionAsync(response.Reaction);
                return;
            }

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
        }

        [Command("resume")]
        [Description("Resume playback.")]
        public async Task Resume(CommandContext ctx)
        {
            var response = await LavaLinkCommandsCore.ResumeCommand(ctx.Client, ctx.Guild, ctx.Channel, ctx.Member);

            if (response.Reaction != null)
            {
                await ctx.Message.CreateReactionAsync(response.Reaction);
                return;
            }

            if (response.IsEmptyResponse) return;

            await ctx.RespondAsync(response.GetMessageBuilder());
        }

        [Command("join")]
        [Description("Makes the bot join a specific voice channel")]
        public async Task Join(CommandContext ctx, DiscordChannel channel)
        {
            if(await ConnectToVoice(ctx, channel, true, MusicQueueManager) != null)
            {
                await ctx.RespondAsync($"Joined {channel.Name}!");
            }
                
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            DiscordChannel channel = ctx?.Member?.VoiceState?.Channel;

            if(channel == null || channel.Type != ChannelType.Voice || (channel?.Guild != null && channel.Guild.IsUnavailable))
            {
                await ctx.RespondAsync("You aren't connected to a voice channel!");
                return;
            }

            var emoji = Config.GetGuildEmojiOrFallback(ctx.Client, BotConfig.CustomReactionSettings.JoinCommandReactionId, "⤴️");

            await ctx.Message.CreateReactionAsync(emoji);

            await ConnectToVoice(ctx, channel, true, MusicQueueManager);
        }

        [Command("leave")]
        [Description("Makes the bot leave the current voice channel")]
        public async Task Leave(CommandContext ctx)
        {
            var conn = await GetGuildConnection(ctx);

            if (conn == null) return;

            DiscordChannel channel = ctx?.Member?.VoiceState?.Channel;

            if (channel == null || channel.Type != ChannelType.Voice || channel != conn.Channel)
            {
                await ctx.RespondAsync("You aren't connected to the same voice channel!");
                return;
            }

            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx?.Guild);

            if (conn.CurrentState.CurrentTrack != null && queue != null)
            {
                queue.SaveLastDequeuedSongTime(conn.CurrentState.PlaybackPosition);
            }

            var emoji = Config.GetGuildEmojiOrFallback(ctx.Client, BotConfig.CustomReactionSettings.LeaveCommandReactionId, "👋");

            await ctx.Message.CreateReactionAsync(emoji);

            await conn.DisconnectAsync();
        }

        [Obsolete]
        private static async Task<LavalinkGuildConnection> ConnectToVoice(DiscordClient client, DiscordMember member, DiscordChannel channel, MusicQueueManager musicQueueManager, CommandContext ctx = null)
        {
            var lava = client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                if (ctx != null)
                    await ctx.RespondAsync("The Lavalink connection is not established");
                return null;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel?.Type != ChannelType.Voice)
            {
                if (ctx != null)
                    await ctx.RespondAsync("Not a valid voice channel.");
                return null;
            }

            var conn = await node.ConnectAsync(channel);

            musicQueueManager?.OnConnected(conn);

            return conn;
        }

        [Obsolete]
        private static async Task<LavalinkGuildConnection> ConnectToVoice(CommandContext ctx, DiscordChannel channel, bool sendErrorMessages, MusicQueueManager musicQueueManager)
        {
            return await ConnectToVoice(ctx.Client, ctx.Member, channel == null ? ctx.Member?.VoiceState?.Channel : channel, musicQueueManager, sendErrorMessages ? ctx : null);
        }

        [Obsolete]
        public static async Task<LavalinkGuildConnection> GetGuildConnection(DiscordClient client, DiscordMember member, CommandContext ctx = null, bool tryToConnect = false, DiscordChannel voiceChannel = null, MusicQueueManager musicQueueManager = null)
        {
            if (member.VoiceState == null || member.VoiceState.Channel == null)
            {
                if (ctx != null)
                    await ctx.RespondAsync("You are not in a voice channel.");
                return null;
            }

            var lava = client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(member.VoiceState.Guild);

            if (conn == null)
            {
                if (tryToConnect && voiceChannel != null)
                {
                    conn = await ConnectToVoice(client, member, voiceChannel, musicQueueManager, ctx);
                    if (conn == null)
                    {
                        return null;
                    }
                    return conn;
                }
                if (ctx != null)
                    await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return null;
            }

            return conn;
        }

        [Obsolete]
        public static async Task<LavalinkGuildConnection> GetGuildConnection(CommandContext ctx, bool sendErrorMessages = true, bool tryToConnect = false, DiscordChannel voiceChannel = null, MusicQueueManager musicQueueManager = null)
        {
            return await GetGuildConnection(ctx.Client, ctx.Member, sendErrorMessages ? ctx : null, tryToConnect, voiceChannel, musicQueueManager);
        }
    }
}
