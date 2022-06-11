using DiscordBot.Attributes;
using DiscordBot.Events;
using DiscordBot.Extensions;
using DiscordBot.Managers;
using DiscordBot.Models.Configuration;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static DiscordBot.Events.CommandResponse;
using static DiscordBot.Managers.MusicQueueManager;

namespace DiscordBot.Commands.Core
{
    [AutoDI.SingletonCreateAndInstall]
    public class LavaLinkCommandsCore
    {
        #region other_stuff
        private static string _streamProgressBar = string.Empty;
        public static string StreamProgressBar
        {
            get
            {
                if (string.IsNullOrEmpty(_streamProgressBar))
                    _streamProgressBar = Utilities.GetAlternatingBar(20, "🎵", "🎹");
                return _streamProgressBar;
            }
        }
        public static CommandResponse NotPlayingAnything { get; private set; } = new CommandResponse
        {
            Embed = Utilities.CreateErrorEmbed("Not playing anything.")
        };
        public static CommandResponse NoTracksLoaded { get; private set; } = new CommandResponse
        {
            Embed = Utilities.CreateErrorEmbed("There are no tracks loaded.")
        };
        public static CommandResponse NotInSameChannel { get; private set; } = new CommandResponse
        {
            Embed = Utilities.CreateErrorEmbed("You have to be in the voice channel to control the bot.")
        };
        public static CommandResponse BotNotConnected { get; private set; } = new CommandResponse
        {
            Embed = Utilities.CreateErrorEmbed("I am not connected to a voice channel.")
        };
        #endregion other_stuff

        public Config BotConfig { private get; set; }
        public MusicQueueManager MusicQueueManager { private get; set; }
        public EqualizerManager EqualizerManager { private get; set; }


        public bool TryReloadTrackIfNecessary(LavalinkNodeConnection node, TrackInfo trackInfo, ref LavalinkTrack track, out CommandResponse errorResponse)
        {
            if (track == null && trackInfo?.Uri != null)
            {
                Log.Debug($"Reloading track from Uri {trackInfo.Uri}");
                if (!TrySearchOrLoadTrack(trackInfo.Uri, node, out var finalLoadResult, out errorResponse))
                {
                    Log.Debug($"Reloading track from Uri {trackInfo.Uri} FAILED");
                    return false;
                }
                track = finalLoadResult.Tracks.FirstOrDefault();
            }
            errorResponse = null;
            return true;
        }

        public static bool TrySearchOrLoadTrack(string searchOrUrl, LavalinkNodeConnection node, out LavalinkLoadResult finalLoadResult, out CommandResponse commandResponse)
        {
            Log.Debug($"Loading track from search or url \"{searchOrUrl}\"");
            LavalinkLoadResult loadResult = null;
            if (searchOrUrl.StartsWith("https://") || searchOrUrl.StartsWith("http://"))
            {
                var uri = new Uri(searchOrUrl);
                loadResult = node.Rest.GetTracksAsync(uri).Result;
            }
            else if (searchOrUrl.Contains("soundcloud"))
            {
                searchOrUrl = searchOrUrl.Replace("soundcloud", string.Empty);
                loadResult = node.Rest.GetTracksAsync(searchOrUrl, LavalinkSearchType.SoundCloud).Result;
            }

            if (loadResult != null && loadResult.LoadResultType != LavalinkLoadResultType.LoadFailed && loadResult.LoadResultType != LavalinkLoadResultType.NoMatches)
            {
                finalLoadResult = loadResult;
            }
            else
            {
                // Search YouTube instead
                finalLoadResult = node.Rest.GetTracksAsync(searchOrUrl, LavalinkSearchType.Youtube).Result;
            }

            if (finalLoadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || finalLoadResult.LoadResultType == LavalinkLoadResultType.NoMatches
                || finalLoadResult.Tracks.Count() == 0)
            {
                string error = finalLoadResult.Exception.Message;

                if (string.IsNullOrWhiteSpace(error))
                {
                    error = loadResult?.Exception.Message;
                    if (string.IsNullOrWhiteSpace(error))
                    {
                        error = "Track loading failed";
                    }
                }

                finalLoadResult = null;
                commandResponse = new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed($"Track search failed for {searchOrUrl}. ({error})")
                };
                return false;
            }
            commandResponse = null;
            return true;
        }

        public async Task<CommandResponse> PlayCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker, string searchOrUrl, bool autoConnect = true)
        {
            if (string.IsNullOrWhiteSpace(searchOrUrl)) return CommandResponse.Empty;

            if (!TryGetGuildConnection(client, guild, out var conn, out var node))
            {
                var wrapper = new CommandResponseWrapper();
                conn = await ConnectToMemberVoice(client, invoker, autoConnect, wrapper);
                if (conn == null) return wrapper.ResponseOrEmpty;
            }

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);

            if(!musicPlayerData.AllowNonPresentMemberControl)
            {
                if(!IsMemberInMusicChannel(conn, invoker))
                {
                    return NotInSameChannel;
                }
            }

            if(!TrySearchOrLoadTrack(searchOrUrl, node, out var finalLoadResult, out var errorResponse))
            {
                return errorResponse;
            }

            var queue = musicPlayerData.Queue;

            var track = finalLoadResult.Tracks.First();

            musicPlayerData.LastUsedPlayControlChannel = invokerMessageChannel;

            if (finalLoadResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                string playlistName = finalLoadResult.PlaylistInfo.Name;

                string extra = string.Empty;

                var timeUntil = queue.EnqueueTracks(finalLoadResult.Tracks);

                if (!IsTrackLoaded(conn))
                {
                    var trackInfo = queue.DequeueTrack();
                    track = trackInfo.Track;

                    if(!TryReloadTrackIfNecessary(node, trackInfo, ref track, out errorResponse))
                    {
                        return errorResponse;
                    }

                    await conn.PlayAsync(track);
                    extra = $"Now playing `{track.Title}`!\n";
                }
                else
                {
                    timeUntil = timeUntil + conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition;
                }

                Log.Information($"Queuing playlist '{playlistName}' ({finalLoadResult.Tracks.Count()} Tracks) in '{conn.Guild.Name}' / '{conn.Guild.Id}'.");

                return new CommandResponse
                {
                    Embed = Utilities.CreateSuccessEmbed($"{extra}Added **{finalLoadResult.Tracks.Count() - 1}** songs from playlist `{playlistName}` to the queue! 🎵", searchOrUrl)
                };
            }


            if (IsTrackLoaded(conn))
            {
                var timeUntil = queue.EnqueueTrack(track) + conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition;

                return new CommandResponse
                {
                    Embed = Utilities.CreateSuccessEmbed($"Added `{track.Title}` to the queue! {(!queue.IsRandomMode ? $"Estimated time until playback: {Utilities.SpecialFormatTimeSpan(timeUntil)}" : string.Empty)} 🎵", track.Uri.ToString())
                };
            }

            queue.EnqueueTrack(track);

            var trackInfoFromQueue = queue.DequeueTrack();
            track = trackInfoFromQueue.Track;

            if (!TryReloadTrackIfNecessary(node, trackInfoFromQueue, ref track, out errorResponse))
            {
                return errorResponse;
            }

            await conn.PlayAsync(track);

            return new CommandResponse
            {
                Embed = Utilities.CreateSuccessEmbed($"Now playing `{track.Title}`! 🎵", track.Uri.ToString())
            };
        }

        public async Task<CommandResponse> PlayLastTrackCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);
            var queue = musicPlayerData.Queue;

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(client, guild, invoker))
                {
                    return NotInSameChannel;
                }
            }

            var lastTrack = queue.LastDequeuedTrack;

            if (lastTrack == null)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"There is no last track to re-play.")
                };
            }

            queue.EnqueueTrack(lastTrack);

            if (!TryGetGuildConnection(client, guild, out var conn, out var node))
            {
                var wrapper = new CommandResponseWrapper();
                conn = await ConnectToMemberVoice(client, invoker, true, wrapper);
                if (conn == null) return wrapper.ResponseOrEmpty;
            }


            if (!IsTrackLoaded(conn))
            {
                var nextTrack = queue.DequeueTrack();

                if (nextTrack == null)
                {
                    return new CommandResponse
                    {
                        Embed = Utilities.CreateErrorEmbed($"Sorry, something went wrong.")
                    };
                }

                musicPlayerData.LastUsedPlayControlChannel = invokerMessageChannel;

                LavalinkTrack track = nextTrack.Track;

                if(!TryReloadTrackIfNecessary(node, nextTrack, ref track, out var errorResponse))
                {
                    return errorResponse;
                }

                await conn.PlayAsync(track);

                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"Replaying last song: `{nextTrack.Track?.Title}`.", nextTrack.Uri.ToString())
                };
            }

            return new CommandResponse
            {
                Embed = Utilities.CreateInfoEmbed($"Added last played track `{lastTrack?.Track?.Title}` to the queue.", lastTrack.Uri.ToString())
            };
        }

        public async Task<CommandResponse> ForceSkipCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            TryGetGuildConnection(client, guild, out var conn, out var node);

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);
            var queue = musicPlayerData.Queue;

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(conn, invoker))
                {
                    return new CommandResponse
                    {
                        Embed = Utilities.CreateErrorEmbed("You have to be in the voice channel to control the bot.")
                    };
                }
            }

            if (!IsTrackLoaded(conn))
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"There is nothing to skip.")
                };
            }

            var title = conn.CurrentState.CurrentTrack.Title;
            var link = conn.CurrentState.CurrentTrack.Uri.ToString();

            if (!queue.IsEmpty)
            {
                var trackInfo = queue.DequeueTrack();
                var track = trackInfo.Track;
                
                if (!TryReloadTrackIfNecessary(node, trackInfo, ref track, out var errorResponse))
                {
                    return errorResponse;
                }

                Log.Logger.Information($"Now playing  \"{track?.Title}\".");

                await conn.PlayAsync(track);
            }
            else
            {
                await conn.StopAsync();
            }

            return new CommandResponse
            {
                Embed = Utilities.CreateTitleEmbed($"⏩ Skipped: `{title}`", DiscordColor.IndianRed, link)
            };
        }

        public async Task<CommandResponse> VolumeCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker, int? volume)
        {
            TryGetGuildConnection(client, guild, out var conn);

            var eqsettings = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(guild);

            if (!volume.HasValue || conn == null)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"Current Volume is at **{eqsettings.Volume}**! 🔊")
                };
            }

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(conn, invoker))
                {
                    return NotInSameChannel;
                }
            }

            if (volume < 0 || volume > 1000)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed("Volume provided is out of range! (0 - 1000) 🔇")
                };
            }

            eqsettings.Volume = volume.Value;

            await conn.SetVolumeAsync(volume.Value);

            return new CommandResponse
            {
                Embed = Utilities.CreateSuccessEmbed($"Volume set to **{volume}**! 🔊")
            };
        }

        public async Task<CommandResponse> PauseCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            if(!TryGetGuildConnection(client, guild, out var conn))
            {
                return NotPlayingAnything;
            }

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);
            var queue = musicPlayerData.Queue;

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(conn, invoker))
                {
                    return NotInSameChannel;
                }
            }

            if (!IsTrackLoaded(conn))
            {
                return NoTracksLoaded;
            }

            if(conn.CurrentState.PlaybackPosition.TotalSeconds >= 3)
                queue.SaveLastDequeuedSongTime(conn.CurrentState.PlaybackPosition);

            var emoji = Config.GetGuildEmojiOrFallback(client, BotConfig.CustomReactionSettings.PauseCommandReactionId, "⏸️");

            await conn.PauseAsync();

            return new CommandResponse
            {
                Reaction = emoji,
                Embed = Utilities.CreateInfoEmbed($"Playback paused. {emoji}")
            };
        }

        public async Task<CommandResponse> ResumeCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            if (!TryGetGuildConnection(client, guild, out var conn, out var node))
            {
                var wrapper = new CommandResponseWrapper();
                conn = await ConnectToMemberVoice(client, invoker, true, wrapper);
                if (conn == null) return wrapper.ResponseOrEmpty;
            }

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);
            var queue = musicPlayerData.Queue;

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(conn, invoker))
                {
                    return NotInSameChannel;
                }
            }

            if (!IsTrackLoaded(conn))
            {
                var track = queue.LastDequeuedTrack?.Track;
                if (track != null && queue.LastDequeuedSongTime.TotalSeconds > 2)
                {
                    Log.Information($"Starting song from position: {Utilities.SpecialFormatTimeSpan(queue.LastDequeuedSongTime)}");
                    await conn.PlayPartialAsync(track, queue.LastDequeuedSongTime, track.Length);
                    queue.SaveLastDequeuedSongTime(new TimeSpan());
                    return new CommandResponse {
                        Embed = Utilities.CreateInfoEmbed($"Resuming from queue with `{track.Title}`!")
                    };
                }
                return NoTracksLoaded;
            }

            var emoji = Config.GetGuildEmojiOrFallback(client, BotConfig.CustomReactionSettings.ResumeCommandReactionId, "▶️");

            await conn.ResumeAsync();

            return new CommandResponse
            {
                Reaction = emoji,
                Embed = Utilities.CreateInfoEmbed($"Resuming playback. {emoji}")
            };
        }

        public async Task<CommandResponse> LeaveCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            if (!TryGetGuildConnection(client, guild, out var conn))
            {
                return BotNotConnected;
            }

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(conn, invoker))
                {
                    return NotInSameChannel;
                }
            }

            if (IsTrackLoaded(conn))
            {
                musicPlayerData.Queue.SaveLastDequeuedSongTime(conn.CurrentState.PlaybackPosition);
            }

            await conn.DisconnectAsync();

            var emoji = Config.GetGuildEmojiOrFallback(client, BotConfig.CustomReactionSettings.LeaveCommandReactionId, "👋");

            return new CommandResponse
            {
                Reaction = emoji,
                Embed = Utilities.CreateInfoEmbed($"{emoji}")
            };
        }

        // TODO
        public async Task<CommandResponse> EqualizerApplyProfileCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker, string profile)
        {
            var eqsettings = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(guild);

            var invalidProfile = new CommandResponse
            {
                Embed = Utilities.CreateErrorEmbed($"Invalid or unknown profile '`{profile}`' provided!")
            };

            if (string.IsNullOrWhiteSpace(profile)) return invalidProfile;

            if(!TryGetGuildConnection(client, guild, out var conn))
            {
                return BotNotConnected;
            }

            if(profile == EqualizerManager.RESET_PROFILE)
            {
                eqsettings.Reset();
                await conn.ResetEqualizerAsync();
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed("Equalizer has been reset!")
                };
            }

            // TODO
            // EqualizerManager . GetProfile(profile) ?
            return CommandResponse.Empty;
        }

        public CommandResponse NowPlayingCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            if(!TryGetGuildConnection(client, guild, out var conn))
            {
                return NotPlayingAnything;
            }

            if(!IsTrackLoaded(conn))
            {
                return NotPlayingAnything;
            }

            var track = conn.CurrentState.CurrentTrack;

            var pos = conn.CurrentState.PlaybackPosition;
            var end = track.Length;

            if (track == null || pos == null) return NotPlayingAnything;

            string progressBar = Utilities.GetTextProgressBar((float) track.Position.TotalMilliseconds, (float) end.TotalMilliseconds, (float) pos.TotalMilliseconds, "🟪", "🎶", "▪️");
            string textProgressCurrent = Utilities.SpecialFormatTimeSpan(pos, end);
            string textProgressEnd = Utilities.SpecialFormatTimeSpan(end);
            string textProgress = $"{textProgressCurrent} / {(conn.CurrentState.CurrentTrack.IsStream ? "Live 🔴" : textProgressEnd)}";

            var embed = new DiscordEmbedBuilder()
                .WithTitle(track.Title)
                .WithAuthor(track.Author)
                .WithUrl(track.Uri)
                .AddField($"Progress ({textProgress})", conn.CurrentState.CurrentTrack.IsStream ? StreamProgressBar : progressBar)
                .WithTimestamp(DateTime.Now);

            var trackUriString = track.Uri.ToString();
            if (trackUriString.Contains("youtube.com"))
            {
                string ytId = trackUriString.Substring(trackUriString.Length - 11, 11);
                embed.WithThumbnail($"https://i3.ytimg.com/vi/{ytId}/maxresdefault.jpg");
                embed.WithColor(DiscordColor.Red);
            }

            return new CommandResponse
            {
                Embed = embed
            };
        }

        public CommandResponse EqualizerCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            var eqsettings = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(guild);

            var eqOffset = EQOffset.Lows;

            return new CommandResponse {
                Embed = InteractionHandler.CreateEQSettingsEmbed(eqsettings, eqOffset, InteractionHandler.EditingState.Saved),
                Components = InteractionHandler.CreateEQSettingsComponents(eqsettings, eqOffset)
            };
        }

        public CommandResponse ShuffleQueueCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);
            var queue = musicPlayerData.Queue;

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(client, guild, invoker))
                {
                    return NotInSameChannel;
                }
            }

            if (queue.Count <= 1)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed("There is nothing to shuffle!")
                };
            }

            queue.Shuffle();

            var embed = Utilities.CreateSuccessEmbed("The Queue has been shuffled! 🎲");

            if (queue.IsRandomMode)
            {
                embed.Footer.Text = $"The Queue is in {queue.Mode}, there's no need to shuffle.";
            }

            return new CommandResponse
            {
                Embed = embed
            };
        }

        public CommandResponse QueueModeCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker, string queueModeString)
        {
            MusicQueueManager.QueueMode queueMode = MusicQueueManager.QueueMode.Default;

            try
            {
                queueMode = Enum.Parse<MusicQueueManager.QueueMode>(queueModeString);
            }
            catch (Exception)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed($"Provided Mode doesn't exist! Available Modes are: [{string.Join(", ", Enum.GetNames(typeof(MusicQueueManager.QueueMode)))}]")
                };
            }

            return QueueModeCommand(client, guild, invokerMessageChannel, invoker, queueMode);
        }

        public CommandResponse QueueModeCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker, MusicQueueManager.QueueMode queueMode)
        {
            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(client, guild, invoker))
                {
                    return NotInSameChannel;
                }
            }

            musicPlayerData.Queue.Mode = queueMode;

            if (QueueModeReactions.TryGetValue(queueMode, out DiscordEmoji emoji))
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"Switched Queue Mode to `{queueMode}`! {emoji}"),
                    Reaction = emoji
                };
            }

            return new CommandResponse
            {
                Embed = Utilities.CreateInfoEmbed($"Switched Queue Mode to `{queueMode}`!")
            };
        }

        public CommandResponse ClearQueueCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            if (!TryGetGuildConnection(client, guild, out var conn))
            {
                return BotNotConnected;
            }

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);
            var queue = musicPlayerData.Queue;

            if (!musicPlayerData.AllowNonPresentMemberControl)
            {
                if (!IsMemberInMusicChannel(client, guild, invoker))
                {
                    return NotInSameChannel;
                }
            }

            if (queue.IsEmpty)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed("Nothing in the queue!")
                };
            }

            var numCleared = queue.Clear();

            return new CommandResponse
            {
                Embed = Utilities.CreateSuccessEmbed($"The queue has been cleared and {numCleared} songs have been sent to the shadow realm!")
            };
        }

        public CommandResponse QueueCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(guild);

            var nextSongs = queue.GetTopXTracks(10);

            string extra = string.Empty;
            if (QueueModeReactions.TryGetValue(queue.Mode, out var discordEmoji))
            {
                extra = $" {discordEmoji}";
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle($"Queue ({(queue.Count == 0 ? "Empty" : $"{queue.Count} Track{(queue.Count == 1 ? string.Empty : "s")}")}) | {queue.Mode.ToString()} Mode{extra}")
                .WithTimestamp(DateTime.Now);


            var listOfStrings = new List<string>();
            int totalCharacterCount = 0;

            TryGetGuildConnection(client, guild, out var conn);

            TimeSpan currentTimeUntil = new TimeSpan();

            if (conn != null && conn.CurrentState.CurrentTrack != null)
            {
                embed.WithAuthor($"Current Song: {conn.CurrentState.CurrentTrack.Title}", conn.CurrentState.CurrentTrack.Uri.ToString());
                currentTimeUntil = conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition;
            }

            if (queue.Count == 0)
            {
                embed.WithDescription($"> No tracks {(conn?.CurrentState?.CurrentTrack != null ? "*(except for the currently playing one)* " : string.Empty)}left in the queue!\n> Add some more with the `play` command.");

                return new CommandResponse
                {
                    Embed = embed
                };
            }

            foreach (var trackInfo in nextSongs)
            {
                string newString = $"{trackInfo?.Track?.Title} | {(queue.IsRandomMode ? "??:??" : Utilities.SpecialFormatTimeSpan(currentTimeUntil))}";
                totalCharacterCount += newString.Length;
                if (totalCharacterCount > 4000) break;
                currentTimeUntil += trackInfo?.Track?.Length ?? TimeSpan.Zero;
                listOfStrings.Add(newString);
            }

            string description = Utilities.GetListAsAlternatingStringWithLinebreaks(listOfStrings, Utilities.EmojiNumbersFromOneToTen);

            embed.WithDescription(description);

            embed.WithFooter($"Queue length: {Utilities.SpecialFormatTimeSpan(queue.GetTotalPlayTime())}");

            return new CommandResponse
            {
                Embed = embed
            };
        }

        #region util
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

        public async Task<LavalinkGuildConnection> ConnectToMemberVoice(DiscordClient client, DiscordMember invoker, bool autoConnect, CommandResponseWrapper responseWrapper = null)
        {
            if (!autoConnect)
            {
                responseWrapper.SetResponse(BotNotConnected);
                return null;
            }

            var channel = invoker.VoiceState?.Channel;

            if (channel == null) return null;

            return await ConnectToVoice(client, channel, responseWrapper);
        }

        public async Task<LavalinkGuildConnection> ConnectToVoice(DiscordClient client, DiscordChannel channelToJoin, CommandResponseWrapper responseWrapper = null)
        {
            if(channelToJoin == null)
            {
                responseWrapper.SetResponse(new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed($"Unable to join your voice channel.")
                });
                return null;
            }

            if (!TryGetNode(client, out var node))
            {
                responseWrapper.SetResponse(new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed("The Lavalink connection is not established.")
                });
                return null;
            }

            if (channelToJoin?.Type != ChannelType.Voice)
            {
                responseWrapper.SetResponse(new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed($"\"{channelToJoin?.Name}\" is not a valid voice channel.")
                });
                return null;
            }

            var conn = await node.ConnectAsync(channelToJoin);

            MusicQueueManager.OnConnected(conn);

            return conn;
        }

        public async Task<LavalinkGuildConnection> JoinMemberVoice(DiscordClient client, DiscordMember member, bool memberIsInvoker = false, CommandResponseWrapper responseWrapper = null)
        {
            var channelToJoin = member?.VoiceState?.Channel;

            if(channelToJoin == null)
            {
                responseWrapper.SetResponse(new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed(memberIsInvoker ? $"You don't seem to be in a voice channel." : $"{member.Username} doesn't seem to be in a voice channel.")
                });
                return null;
            }

            return await ConnectToVoice(client, channelToJoin, responseWrapper);
        }

        public static bool TryGetNode(DiscordClient client, out LavalinkNodeConnection node)
        {
            var lava = client.GetLavalink();
            if (!lava.ConnectedNodes.Any())
            {
                node = null;
                return false;
            }
            node = lava.ConnectedNodes.Values.First();
            return true;
        }

        public static bool TryGetGuildConnection(DiscordClient client, DiscordGuild guild, out LavalinkGuildConnection connection, out LavalinkNodeConnection node)
        {
            if(!TryGetNode(client, out node))
            {
                connection = null;
                return false;
            }
            connection = node.GetGuildConnection(guild);

            return connection != null;
        }

        public static bool TryGetGuildConnection(DiscordClient client, DiscordGuild guild, out LavalinkGuildConnection connection)
        {
            return TryGetGuildConnection(client, guild, out connection, out var _);
        }

        public static bool IsTrackLoaded(LavalinkGuildConnection conn)
        {
            if (conn == null) return false;

            if(conn.CurrentState.CurrentTrack != null)
            {
                return true;
            }

            return false;
        }

        public static bool IsMemberInVoiceChannel(DiscordMember member)
        {
            if (member.VoiceState?.Channel == null)
            {
                return false;
            }
            return true;
        }

        public static bool IsMemberInMusicChannel(DiscordClient client, DiscordGuild guild, DiscordMember member)
        {
            if (!TryGetGuildConnection(client, guild, out var conn, out var _))
            {
                return false;
            }

            return IsMemberInMusicChannel(conn, member);
        }

        public static bool IsMemberInMusicChannel(LavalinkGuildConnection conn, DiscordMember member)
        {
            if (conn.Channel == member.VoiceState?.Channel)
            {
                return true;
            }

            return false;
        }

        public static bool IsBotConnected(DiscordClient client, DiscordGuild guild)
        {
            return TryGetGuildConnection(client, guild, out var _, out var _);
        }

        public static async Task ForceDisconnect(DiscordClient client, DiscordGuild guild)
        {
            TryGetGuildConnection(client, guild, out var conn);

            await conn?.DisconnectAsync();
        }
        #endregion util
    }
}
