using DiscordBot.Attributes;
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
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    public class LavaLinkCommandsModule : BaseCommandModule
    {
        public EqualizerManager EqualizerManager { private get; set; }
        public MusicQueueManager MusicQueueManager { private get; set; }

        [Command("play")]
        [Description("Play a video from YouTube")]
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
                conn = await ConnectToVoice(ctx, ctx?.Member?.VoiceState?.Channel, false, MusicQueueManager);
                if (conn == null)
                {
                    await ctx.RespondAsync("I'm not connected to a Voice Channel.");
                    return;
                }
            }

            LavalinkLoadResult loadResult = null;
            if(search.StartsWith("https://") || search.StartsWith("http://"))
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

                    Log.Information($"Playing '{track.Title}' in '{conn.Guild.Name}' / '{conn.Guild.Id}' from queue.");
                    await conn.PlayAsync(track);
                    extra = $"Now playing `{track.Title}`!\n";
                }
                else
                {
                    timeUntil = timeUntil + conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition;
                }

                await ctx.RespondAsync($"{extra}Added **{loadResultSearch.Tracks.Count()-1}** songs from playlist `{playlistName}` to the queue! 🎵");
                return;
            }
            

            if (conn.CurrentState.CurrentTrack != null)
            {
                var timeUntil = queue.EnqueueTrack(track) + conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition;

                await ctx.RespondAsync($"Added `{track.Title}` to the queue! {(!queue.IsRandomMode ? $"Estimated time until playback: {Utilities.SpecialFormatTimeSpan(timeUntil)}" : string.Empty)} 🎵");
                return;
            }

            queue.EnqueueTrack(track);

            await conn.PlayAsync(queue.DequeueTrack());

            await ctx.RespondAsync($"Now playing `{track.Title}`! 🎵");
        }

        [Command("last-song")]
        [Aliases("last")]
        public async Task LastSong(CommandContext ctx)
        {
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx.Guild);

            var lastTrack = queue.LastDequeuedTrack;

            if(lastTrack == null)
            {
                await ctx.RespondAsync($"There is no last track to re-play.");
                return;
            }

            queue.EnqueueTrack(lastTrack);

            var conn = await GetGuildConnection(ctx, true, true, ctx.Member.VoiceState.Channel, MusicQueueManager);

            if (conn == null) return;

            if(conn.CurrentState.CurrentTrack == null)
            {
                var nextTrack = queue.DequeueTrack();

                if(nextTrack == null)
                {
                    await ctx.RespondAsync($"Sorry, something went wrong.");
                    return;
                }

                await conn.PlayAsync(nextTrack);

                await ctx.RespondAsync($"Replaying last song: `{nextTrack.Title}`");

                return;
            }

            await ctx.RespondAsync($"Added last played track `{lastTrack?.Title}` to the queue!");
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

        private static Dictionary<MusicQueueManager.QueueMode, DiscordEmoji> _queueModeReactions = null;
        public static Dictionary<MusicQueueManager.QueueMode, DiscordEmoji> QueueModeReactions
        {
            get
            {
                if (_queueModeReactions == null)
                {
                    _queueModeReactions = new Dictionary<MusicQueueManager.QueueMode, DiscordEmoji>();

                    Type type = typeof(MusicQueueManager.QueueMode);
                    foreach (string name in Enum.GetNames(type))
                    {
                        if (name != null)
                        {
                            FieldInfo field = type.GetField(name);
                            if (field != null)
                            {
                                AttachedStringAttribute attr = Attribute.GetCustomAttribute(field, typeof(AttachedStringAttribute)) as AttachedStringAttribute;
                                if (attr != null)
                                {
                                    var enumValue = Enum.Parse<MusicQueueManager.QueueMode>(name);
                                    try
                                    {
                                        _queueModeReactions.Add(enumValue, DiscordEmoji.FromUnicode(attr.Value));
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error($"Non valid emoji attached to {type.FullName}.{enumValue}! {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                return _queueModeReactions;
            }
        }

        [Command("queue-mode")]
        [Aliases("qm", "queuemode")]
        public async Task QueueMode(CommandContext ctx, string queueModeString)
        {
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(ctx?.Guild);

            if (queue == null) return;
            try
            {
                queue.Mode = Enum.Parse<MusicQueueManager.QueueMode>(queueModeString);
            }
            catch(Exception)
            {
                await ctx.RespondAsync($"Provided Mode doesn't exist! Available Modes are: [{string.Join(", ", Enum.GetNames(typeof(MusicQueueManager.QueueMode)))}]");
                return;
            }

            if(QueueModeReactions.TryGetValue(queue.Mode, out DiscordEmoji emoji))
            {
                await ctx.Message.CreateReactionAsync(emoji);
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

            string extra = string.Empty;
            if(QueueModeReactions.TryGetValue(queue.Mode, out var discordEmoji))
            {
                extra = $" {discordEmoji}";
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Queue ({(queue.Count == 0 ? "Empty" : $"{queue.Count} Track{(queue.Count == 1 ? string.Empty : "s")}")}) | {queue.Mode.ToString()} Mode{extra}")
                .WithTimestamp(DateTime.Now);


            var listOfStrings = new List<string>();
            int totalCharacterCount = 0;
            
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx, false);

            TimeSpan currentTimeUntil = new TimeSpan();

            if (conn != null && conn.CurrentState.CurrentTrack != null)
            {
                embed.WithAuthor($"Current Song: {conn.CurrentState.CurrentTrack.Title}", conn.CurrentState.CurrentTrack.Uri.ToString());
                currentTimeUntil = conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition;
            }

            if (nextSongs.Count == 0)
            {
                embed.WithDescription("> No tracks left in the queue!\n> Add some more with the `play` command.");

                var messageEmpty = new DiscordMessageBuilder()
                    .WithEmbed(embed.Build());

                await ctx.RespondAsync(messageEmpty);
                return;
            }

            foreach (LavalinkTrack track in nextSongs)
            {
                string newString = $"{track.Title} | {(queue.IsRandomMode ? "??:??" : Utilities.SpecialFormatTimeSpan(currentTimeUntil))}";
                totalCharacterCount += newString.Length;
                if (totalCharacterCount > 4000) break;
                currentTimeUntil += track.Length;
                listOfStrings.Add(newString);
            }

            string description = Utilities.GetListAsAlternatingStringWithLinebreaks(listOfStrings, Utilities.EmojiNumbersFromOneToTen);

            embed.WithDescription(description);

            embed.WithFooter($"Queue length: {Utilities.SpecialFormatTimeSpan(queue.GetTotalPlayTime())}");

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed.Build());

            await ctx.RespondAsync(message);
        }

        [Command("force-skip")]
        [Aliases("fs", "forceskip")]
        public async Task ForceSkip(CommandContext ctx)
        {
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);

            var title = conn.CurrentState.CurrentTrack.Title;

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"⏩ Skipped: `{title}`")
                .WithUrl(conn.CurrentState.CurrentTrack.Uri)
                .WithColor(DiscordColor.IndianRed);

            //Utilities.EmbedWithUserAuthor(embed, ctx);

            var message = new DiscordMessageBuilder()
                .WithEmbed(embed.Build());

            await conn.SeekAsync(conn.CurrentState.CurrentTrack.Length);
            await ctx.RespondAsync(message);
        }

        [Command("volume")]
        public async Task Volume(CommandContext ctx, [Description("Volume between 0 and 1000")] int volume)
        {
            var conn = await GetGuildConnection(ctx);

            if (conn == null) return;

            var eqsettings = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(ctx.Guild);

            if(volume < 0 || volume > 1000)
            {
                await ctx.RespondAsync("Volume provided is out of range! (0 - 1000) 🔇");
                return;
            }

            eqsettings.Volume = volume;

            await conn.SetVolumeAsync(volume);
            await ctx.RespondAsync($"Volume set to **{volume}**! 🔊");
        }

        [Command("volume")]
        public async Task Volume(CommandContext ctx)
        {
            var conn = await GetGuildConnection(ctx);

            if (conn == null) return;

            var eqsettings = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(ctx.Guild);

            await ctx.RespondAsync($"Current Volume is at **{eqsettings.Volume}**! 🔊");
        }

        [Command("equalizer")]
        [Aliases("eq")]
        public async Task Equalizer(CommandContext ctx)
        {
            var eqsettings = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(ctx.Guild);
            
            var builder = InteractionHandler.BuildEQSettingsMessageWithComponents(eqsettings, EQOffset.Lows, InteractionHandler.EditingState.Saved);

            await ctx.RespondAsync(builder);
        }

        [Command("equalizer")]
        public async Task Equalizer(CommandContext ctx, [RemainingText] string text)
        {
            // TODO: add presets
            if(text.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                var conn = await GetGuildConnection(ctx);

                if (conn == null) return;

                await conn.ResetEqualizerAsync();

                await ctx.RespondAsync("Equalizer has been reset!");

                return;
            }

            await Equalizer(ctx);
        }

        [Command("eq-test")]
        [Hidden]
        public async Task EqTest(CommandContext ctx)
        {
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);

            var equalizer = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(ctx.Guild);


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

        private static string _streamProgressBar = string.Empty;

        [Command("nowplaying")]
        [Aliases("np", "now-playing")]
        public async Task NowPlaying(CommandContext ctx)
        {
            var conn = await GetGuildConnectionCheckTrackPlaying(ctx);

            var track = conn.CurrentState.CurrentTrack;

            var pos = conn.CurrentState.PlaybackPosition;
            var end = track.Length;

            if(string.IsNullOrEmpty(_streamProgressBar))
            {
                _streamProgressBar = Utilities.GetAlternatingBar(20, "🎵", "🎹");
            }

            string progressBar = Utilities.GetTextProgressBar((float) track.Position.TotalMilliseconds, (float) end.TotalMilliseconds, (float) pos.TotalMilliseconds, "🟪", "🎶", "▪️");
            string textProgressCurrent = Utilities.SpecialFormatTimeSpan(pos, end);
            string textProgressEnd = Utilities.SpecialFormatTimeSpan(end);
            string textProgress = $"{textProgressCurrent} / {(conn.CurrentState.CurrentTrack.IsStream ? "Live 🔴" : textProgressEnd)}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle(track.Title)
                .WithAuthor(track.Author)
                .WithUrl(track.Uri)
                .AddField($"Progress ({textProgress})", conn.CurrentState.CurrentTrack.IsStream ? _streamProgressBar : progressBar)
                .WithTimestamp(DateTime.Now);

            var trackUriString = track.Uri.ToString();
            if (trackUriString.Contains("youtube.com"))
            {
                string ytId = trackUriString.Substring(trackUriString.Length-11, 11);
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
            if(await ConnectToVoice(ctx, channel, true, MusicQueueManager) != null)
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

        private static async Task<LavalinkGuildConnection> ConnectToVoice(CommandContext ctx, DiscordChannel channel, bool sendErrorMessages, MusicQueueManager musicQueueManager)
        {
            return await ConnectToVoice(ctx.Client, ctx.Member, ctx.Member.VoiceState.Channel, musicQueueManager, sendErrorMessages ? ctx : null);
        }

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

        public static async Task<LavalinkGuildConnection> GetGuildConnection(CommandContext ctx, bool sendErrorMessages = true, bool tryToConnect = false, DiscordChannel voiceChannel = null, MusicQueueManager musicQueueManager = null)
        {
            return await GetGuildConnection(ctx.Client, ctx.Member, sendErrorMessages ? ctx : null, tryToConnect, voiceChannel, musicQueueManager);
        }

        public static async Task<LavalinkGuildConnection> GetGuildConnectionCheckTrackPlaying(CommandContext ctx, bool sendErrorMessages = true, bool tryToConnect = false, DiscordChannel voiceChannel = null, MusicQueueManager musicQueueManager = null)
        {
            var conn = await GetGuildConnection(ctx, sendErrorMessages, tryToConnect, voiceChannel, musicQueueManager);

            if (conn == null) return null;

            if (conn?.CurrentState?.CurrentTrack == null)
            {
                if (sendErrorMessages)
                    await ctx.RespondAsync("There are no tracks loaded.");
                return null;
            }

            return conn;
        }

    }
}
