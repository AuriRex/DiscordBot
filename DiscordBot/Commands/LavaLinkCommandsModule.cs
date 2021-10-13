using DiscordBot.Managers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    public class LavaLinkCommandsModule : BaseCommandModule
    {
        public EqualizerManager EqualizerManager { private get; set; }
        public MusicQueueManager MusicQueueManager { private get; set; }

        [Command("play")]
        [Description("Play something")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if(search.StartsWith('<') && search.EndsWith('>'))
            {
                search = search.Substring(1, search.Length - 2);
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                conn = await JoinVoice(ctx, ctx?.Member?.VoiceState?.Channel, false);
                if (conn == null)
                {
                    await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                    return;
                }
            }

            conn.PlaybackFinished -= MusicQueueManager.OnPlaybackFinished;
            conn.PlaybackFinished += MusicQueueManager.OnPlaybackFinished;

            LavalinkLoadResult loadResult = null;
            if(search.StartsWith("https://www.youtube.com/"))
            {
                //LavalinkSearchType.
                var uri = new System.Uri(search);
                loadResult = await node.Rest.GetTracksAsync(uri);
            }
            else if (search.Contains("soundcloud"))
            {
                search = search.Replace("soundcloud", string.Empty);
                loadResult = await node.Rest.GetTracksAsync(search, LavalinkSearchType.SoundCloud);
            }

            var loadResultSearch = (loadResult != null && loadResult.LoadResultType != LavalinkLoadResultType.LoadFailed && loadResult.LoadResultType != LavalinkLoadResultType.NoMatches) ? loadResult : await node.Rest.GetTracksAsync(search);

            if (loadResultSearch.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResultSearch.LoadResultType == LavalinkLoadResultType.NoMatches
                || loadResultSearch.Tracks.Count() == 0)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }

            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx.Guild);

            var firstTrack = loadResultSearch.Tracks.First();

            if (loadResultSearch.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                string playlistName = loadResultSearch.PlaylistInfo.Name;
                var otherTracks = new List<LavalinkTrack>(loadResultSearch.Tracks);

                string extra = string.Empty;

                if (conn.CurrentState.CurrentTrack == null)
                {
                    otherTracks.Remove(firstTrack);

                    await conn.PlayAsync(firstTrack);
                    extra = $"Now playing `{firstTrack.Title}`!\n";
                }

                var timeUntil = queue.EnqueueTracks(otherTracks);

                await ctx.RespondAsync($"{extra}Added {otherTracks.Count} from playlist `{playlistName}` to the queue!");
                return;
            }

            

            

            if (conn.CurrentState.CurrentTrack != null)
            {
                var timeUntil = queue.EnqueueTrack(firstTrack);

                await ctx.RespondAsync($"Added `{firstTrack.Title}` to the queue! {(timeUntil != null ? $"Plays in about: {timeUntil.Value.Hours.ToString("00")}:{timeUntil.Value.Minutes.ToString("00")}:{timeUntil.Value.Seconds.ToString("00")}" : string.Empty)}");
                return;
            }

            await conn.PlayAsync(firstTrack);

            await ctx.RespondAsync($"Now playing `{firstTrack.Title}`!");
        }

        [Command("shuffle")]
        public async Task Shuffle(CommandContext ctx)
        {
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx.Guild);

            if(queue.Count <= 1)
            {
                await ctx.RespondAsync("Nothing to shuffle!");
                return;
            }

            if(queue.Mode == MusicQueueManager.QueueMode.Random || queue.Mode == MusicQueueManager.QueueMode.RandomLooping)
            {
                await ctx.RespondAsync($"No need to shuffle the Queue as it is in {queue.Mode} Mode already!");
                return;
            }

            queue.Shuffle();

            await ctx.RespondAsync("The Queue has been shuffled! 🎲");
        }

        [Command("queue")]
        [Aliases("q")]
        public async Task Queue(CommandContext ctx)
        {
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx.Guild);

            var nextSongs = queue.GetTopXTracks(10);

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Queue ({queue.Count} Tracks) | {queue.Mode.ToString()} Mode")
                .WithTimestamp(DateTime.Now);

            var listOfStrings = new List<string>();
            int totalCharacterCount = 0;
            
            var conn = await GetGuildConnection(ctx, false);

            TimeSpan currentTimeUntil = new TimeSpan();

            if (conn != null && conn.CurrentState.CurrentTrack != null)
            {
                currentTimeUntil = conn.CurrentState.CurrentTrack.Length - conn.CurrentState.CurrentTrack.Position;
            }

            foreach(LavalinkTrack track in nextSongs)
            {
                string newString = $"{track.Title} | {Utilities.TimeSpanToString(currentTimeUntil)}";
                totalCharacterCount += newString.Length;
                if (totalCharacterCount > 2000) break;
                currentTimeUntil += track.Length;
                listOfStrings.Add(newString);
            }

            string description = Utilities.GetAlternatingList(listOfStrings, Utilities.EmojiNumbersFromOneToTen);

            embed.WithDescription(description);

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed.Build());

            await ctx.RespondAsync(message);
        }

        [Command("force-skip")]
        [Aliases("fs")]
        public async Task ForceSkip(CommandContext ctx)
        {
            var conn = await GetGuildConnection(ctx);

            var title = conn.CurrentState.CurrentTrack.Title;
            await conn.SeekAsync(conn.CurrentState.CurrentTrack.Length);
            await ctx.RespondAsync($"Skipped `{title}`");
        }

        public async Task<LavalinkGuildConnection> GetGuildConnection(CommandContext ctx, bool respondOnErrors = true)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                if(respondOnErrors)
                    await ctx.RespondAsync("You are not in a voice channel.");
                return null;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                if (respondOnErrors)
                    await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return null;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                if (respondOnErrors)
                    await ctx.RespondAsync("There are no tracks loaded.");
                return null;
            }

            return conn;
        }

        [Command("volume")]
        public async Task SetVolume(CommandContext ctx, int volume)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            if(volume < 0 || volume > 1000)
            {
                await ctx.RespondAsync("Volume provided is out of range! (0 - 1000)");
                return;
            }

            await conn.SetVolumeAsync(volume);
            await ctx.RespondAsync($"Volume set to **{volume}**!");
        }

        [Command("eq-test")]
        [Hidden]
        public async Task EqTest(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }


            var tests = new LavalinkBandAdjustment[]
            {
                new LavalinkBandAdjustment(0, 1),
                new LavalinkBandAdjustment(1, 1),
                new LavalinkBandAdjustment(2, 1),
            };

            await conn.AdjustEqualizerAsync(tests);

            
        }

        [Command("eq-test-reset")]
        [Hidden]
        public async Task EqTestReset(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }


            await conn.ResetEqualizerAsync();
        }

        [Command("nowplaying")]
        [Aliases("np", "now-playing")]
        public async Task NowPlaying(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return;
            }
            var track = conn.CurrentState.CurrentTrack;
            if (track == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            var pos = conn.CurrentState.PlaybackPosition;
            var end = conn.CurrentState.CurrentTrack.Length;

            string progressBar = Utilities.GetTextProgressBar((float) track.Position.TotalMilliseconds, (float) end.TotalMilliseconds, (float) pos.TotalMilliseconds, "🟪", "🎶", "▪️");
            string textProgressCurrent = $"{(end.Hours > 0 ? $"{pos.Hours.ToString("00")}:" : string.Empty)}{pos.Minutes.ToString("00")}:{pos.Seconds.ToString("00")}";
            string textProgressEnd = $"{(end.Hours > 0 ? $"{end.Hours.ToString("00")}:" : string.Empty)}{end.Minutes.ToString("00")}:{end.Seconds.ToString("00")}";
            string textProgress = $"{textProgressCurrent} / {(conn.CurrentState.CurrentTrack.IsStream ? "Live 🔴" : textProgressEnd)}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle(track.Title)
                .WithAuthor(track.Author)
                .WithUrl(track.Uri)
                .AddField($"Progress ({textProgress})", conn.CurrentState.CurrentTrack.IsStream ? Utilities.GetAlternatingBar(20, "🎵", "🎹") : progressBar);

            var trackString = track.Uri.ToString();
            if (trackString.Contains("youtube.com"))
            {
                string ytId = trackString.Substring(trackString.Length-11, 11);
                embed.WithThumbnail($"https://i3.ytimg.com/vi/{ytId}/maxresdefault.jpg");
                embed.WithColor(DiscordColor.Red);
            }

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed.Build());


            await ctx.RespondAsync(message);
        }

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.PauseAsync();
        }

        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
        }

        [Command("join")]
        [Description("Makes the bot join a voice channel")]
        public async Task Join(CommandContext ctx, DiscordChannel channel)
        {
            if(await JoinVoice(ctx, channel, true) != null)
                await ctx.RespondAsync($"Joined {channel.Name}!");
        }

        private async Task<LavalinkGuildConnection> JoinVoice(CommandContext ctx, DiscordChannel channel, bool sendErrorMessages)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                if(sendErrorMessages)
                    await ctx.RespondAsync("The Lavalink connection is not established");
                return null;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel?.Type != ChannelType.Voice)
            {
                if(sendErrorMessages)
                    await ctx.RespondAsync("Not a valid voice channel.");
                return null;
            }

            return await node.ConnectAsync(channel);
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

            await Join(ctx, channel);
        }

        [Command("leave")]
        [Description("Makes the bot leave a voice channel")]
        public async Task Leave(CommandContext ctx, DiscordChannel channel)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

            var conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {channel.Name}!");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            DiscordChannel channel = ctx?.Member?.VoiceState?.Channel;

            if (channel == null || channel.Type != ChannelType.Voice || (channel?.Guild != null && channel.Guild.IsUnavailable))
            {
                await ctx.RespondAsync("You aren't connected to a voice channel!");
                return;
            }

            await Leave(ctx, channel);
        }

    }
}
