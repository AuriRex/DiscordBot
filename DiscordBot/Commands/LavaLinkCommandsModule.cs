using DiscordBot.Managers;
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
            var conn = node.GetGuildConnection(ctx?.Member?.VoiceState?.Guild);

            if (conn == null)
            {
                conn = await ConnectToVoice(ctx, ctx?.Member?.VoiceState?.Channel, false);
                if (conn == null)
                {
                    await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                    return;
                }
            }

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

            var track = loadResultSearch.Tracks.First();

            if (loadResultSearch.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                string playlistName = loadResultSearch.PlaylistInfo.Name;
                
                string extra = string.Empty;

                var timeUntil = queue.EnqueueTracks(loadResultSearch.Tracks);

                if (conn.CurrentState.CurrentTrack == null)
                {
                    track = queue.DequeueTrack();

                    await conn.PlayAsync(track);
                    extra = $"Now playing `{track.Title}`!\n";
                }

                await ctx.RespondAsync($"{extra}And added **{loadResultSearch.Tracks.Count()-1}** songs from playlist `{playlistName}` to the queue! 🎵");
                return;
            }

            

            

            if (conn.CurrentState.CurrentTrack != null)
            {
                var timeUntil = queue.EnqueueTrack(track);

                await ctx.RespondAsync($"Added `{track.Title}` to the queue! {(timeUntil != null ? $"Plays in about: {Utilities.SpecialFormatTimeSpan(timeUntil.Value)}" : string.Empty)} 🎵");
                return;
            }

            await conn.PlayAsync(track);

            await ctx.RespondAsync($"Now playing `{track.Title}`! 🎵");
        }

        [Command("shuffle")]
        public async Task Shuffle(CommandContext ctx)
        {
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx.Guild);

            if(queue.Count <= 1)
            {
                await ctx.RespondAsync("There is nothing to shuffle!");
                return;
            }

            if(queue.IsRandomMode)
            {
                await ctx.RespondAsync($"No need to shuffle the Queue as it is in {queue.Mode} Mode already!");
                return;
            }

            queue.Shuffle();

            await ctx.RespondAsync("The Queue has been shuffled! 🎲");
        }

        [Command("queuemode")]
        [Aliases("qm", "queue-mode")]
        public async Task QueueMode(CommandContext ctx, string queueModeString)
        {
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx?.Guild);

            if (queue == null) return;
            try
            {
                queue.Mode = Enum.Parse<MusicQueueManager.QueueMode>(queueModeString);
            }
            catch(Exception _)
            {
                await ctx.RespondAsync($"Provided Mode doesn't exist! Available Modes are: [{string.Join(", ", Enum.GetNames(typeof(MusicQueueManager.QueueMode)))}]");
                return;
            }

            await ctx.RespondAsync($"Switched Queue Mode to `{queue.Mode}`!");
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
            
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx, false);

            TimeSpan currentTimeUntil = new TimeSpan();

            if (conn != null && conn.CurrentState.CurrentTrack != null)
            {
                currentTimeUntil = conn.CurrentState.CurrentTrack.Length - conn.CurrentState.CurrentTrack.Position;
            }

            foreach(LavalinkTrack track in nextSongs)
            {
                string newString = $"{track.Title} | {(queue.IsRandomMode ? "??:??" : Utilities.SpecialFormatTimeSpan(currentTimeUntil))}";
                totalCharacterCount += newString.Length;
                if (totalCharacterCount > 4000) break;
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
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);

            var title = conn.CurrentState.CurrentTrack.Title;
            await conn.SeekAsync(conn.CurrentState.CurrentTrack.Length);
            await ctx.RespondAsync($"Skipped `{title}` ⏩");
        }

        [Command("volume")]
        public async Task SetVolume(CommandContext ctx, int volume)
        {
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);

            if(volume < 0 || volume > 1000)
            {
                await ctx.RespondAsync("Volume provided is out of range! (0 - 1000) 🔇");
                return;
            }

            await conn.SetVolumeAsync(volume);
            await ctx.RespondAsync($"Volume set to **{volume}**! 🔊");
        }

        [Command("eq-test")]
        [Hidden]
        public async Task EqTest(CommandContext ctx)
        {
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);


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
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);


            await conn.ResetEqualizerAsync();
        }

        [Command("nowplaying")]
        [Aliases("np", "now-playing")]
        public async Task NowPlaying(CommandContext ctx)
        {
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);

            var track = conn.CurrentState.CurrentTrack;

            var pos = conn.CurrentState.PlaybackPosition;
            var end = track.Length;

            string progressBar = Utilities.GetTextProgressBar((float) track.Position.TotalMilliseconds, (float) end.TotalMilliseconds, (float) pos.TotalMilliseconds, "🟪", "🎶", "▪️");
            string textProgressCurrent = Utilities.SpecialFormatTimeSpan(pos, end);
            string textProgressEnd = Utilities.SpecialFormatTimeSpan(end);
            string textProgress = $"{textProgressCurrent} / {(conn.CurrentState.CurrentTrack.IsStream ? "Live 🔴" : textProgressEnd)}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle(track.Title)
                .WithAuthor(track.Author)
                .WithUrl(track.Uri)
                .AddField($"Progress ({textProgress})", conn.CurrentState.CurrentTrack.IsStream ? Utilities.GetAlternatingBar(20, "🎵", "🎹") : progressBar)
                .WithTimestamp(DateTime.Now);

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
            var conn = await GetGuildConnection(ctx);

            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx?.Guild);

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            if(queue != null)
            {
                queue.SaveLastDequeuedSongTime(conn.CurrentState.PlaybackPosition);
            }

            await conn.PauseAsync();
        }

        [Command("resume")]
        public async Task Resume(CommandContext ctx)
        {
            var conn = await GetGuildConnection(ctx, true, true, ctx?.Member?.VoiceState?.Channel);

            if(conn == null)
            {
                await ctx.RespondAsync("An Error occured!");
                return;
            }

            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx?.Guild);

            if (conn.CurrentState.CurrentTrack == null)
            {
                if(queue != null && queue.Count > 0)
                {
                    var track = queue.GetLastTrackLimited();
                    if(track != null)
                    {
                        Log.Information($"Starting song from position: {Utilities.SpecialFormatTimeSpan(queue.LastDequeuedSongTime)}");
                        await conn.PlayPartialAsync(track, queue.LastDequeuedSongTime, track.Length);
                        queue.SaveLastDequeuedSongTime(new TimeSpan());
                        await ctx.RespondAsync($"Resuming from queue with `{track.Title}`!");
                        return;
                    }
                }
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            await conn.ResumeAsync();
        }

        [Command("join")]
        [Description("Makes the bot join a voice channel")]
        public async Task Join(CommandContext ctx, DiscordChannel channel)
        {
            if(await ConnectToVoice(ctx, channel, true) != null)
                await ctx.RespondAsync($"Joined {channel.Name}!");
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
        public async Task Leave(CommandContext ctx)
        {
            var conn = await GetGuildConnection(ctx);

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

            await conn.DisconnectAsync();
        }

        private async Task<LavalinkGuildConnection> ConnectToVoice(CommandContext ctx, DiscordChannel channel, bool sendErrorMessages)
        {
            var lava = ctx.Client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                if (sendErrorMessages)
                    await ctx.RespondAsync("The Lavalink connection is not established");
                return null;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel?.Type != ChannelType.Voice)
            {
                if (sendErrorMessages)
                    await ctx.RespondAsync("Not a valid voice channel.");
                return null;
            }

            var conn = await node.ConnectAsync(channel);

            MusicQueueManager.OnConnected(conn);

            return conn;
        }

        public async Task<LavalinkGuildConnection> GetGuildConnection(CommandContext ctx, bool sendErrorMessages = true, bool tryToConnect = false, DiscordChannel voiceChannel = null)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                if (sendErrorMessages)
                    await ctx.RespondAsync("You are not in a voice channel.");
                return null;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                if(tryToConnect && voiceChannel != null)
                {
                    conn = await ConnectToVoice(ctx, voiceChannel, sendErrorMessages);
                    if (conn == null)
                    {
                        return null;
                    }
                    return conn;
                }
                if (sendErrorMessages)
                    await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                return null;
            }

            return conn;
        }

        public async Task<LavalinkGuildConnection> GetGuildConnectionCheckTrackPlaying(CommandContext ctx, bool sendErrorMessages = true)
        {
            var conn = await GetGuildConnection(ctx, sendErrorMessages);

            if (conn.CurrentState.CurrentTrack == null)
            {
                if (sendErrorMessages)
                    await ctx.RespondAsync("There are no tracks loaded.");
                return null;
            }

            return conn;
        }

    }
}
