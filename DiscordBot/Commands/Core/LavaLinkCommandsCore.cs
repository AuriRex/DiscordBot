using DiscordBot.Attributes;
using DiscordBot.Events;
using DiscordBot.Extensions;
using DiscordBot.Managers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DiscordBot.Events.CommandResponse;

namespace DiscordBot.Commands.Core
{
    [AutoDI.SingletonCreateAndInstall]
    public class LavaLinkCommandsCore
    {
        public MusicQueueManager MusicQueueManager { private get; set; }
        public EqualizerManager EqualizerManager { private get; set; }

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
                if(!IsMemberInMusicChannel(client, guild, invoker))
                {
                    return new CommandResponse
                    {
                        Embed = Utilities.CreateErrorEmbed("You have to be in the voice channel to control the bot.")
                    };
                }
            }

            LavalinkLoadResult loadResult = null;
            if (searchOrUrl.StartsWith("https://") || searchOrUrl.StartsWith("http://"))
            {
                var uri = new Uri(searchOrUrl);
                loadResult = await node.Rest.GetTracksAsync(uri);
            }
            else if (searchOrUrl.Contains("soundcloud"))
            {
                searchOrUrl = searchOrUrl.Replace("soundcloud", string.Empty);
                loadResult = await node.Rest.GetTracksAsync(searchOrUrl, LavalinkSearchType.SoundCloud);
            }

            LavalinkLoadResult finalLoadResult;
            if(loadResult != null && loadResult.LoadResultType != LavalinkLoadResultType.LoadFailed && loadResult.LoadResultType != LavalinkLoadResultType.NoMatches)
            {
                finalLoadResult = loadResult;
            }
            else
            {
                // Search YouTube instead
                finalLoadResult = await node.Rest.GetTracksAsync(searchOrUrl);
            }

            if (finalLoadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || finalLoadResult.LoadResultType == LavalinkLoadResultType.NoMatches
                || finalLoadResult.Tracks.Count() == 0)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed($"Track search failed for {searchOrUrl}. ({finalLoadResult.Exception.Message})")
                };
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
                    track = queue.DequeueTrack();

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

            await conn.PlayAsync(queue.DequeueTrack());

            return new CommandResponse
            {
                Embed = Utilities.CreateSuccessEmbed($"Now playing `{track.Title}`! 🎵", track.Uri.ToString())
            };
        }

        public async Task<CommandResponse> PlayLastTrackCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);
            var queue = musicPlayerData.Queue;

            var lastTrack = queue.LastDequeuedTrack;

            if (lastTrack == null)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"There is no last track to re-play.")
                };
            }

            queue.EnqueueTrack(lastTrack);

            if (!TryGetGuildConnection(client, guild, out var conn))
            {
                var wrapper = new CommandResponseWrapper();
                conn = await ConnectToMemberVoice(client, invoker, true, wrapper);
                if (conn == null) return wrapper.ResponseOrEmpty;
            }


            if (conn.CurrentState.CurrentTrack == null)
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

                await conn.PlayAsync(nextTrack);

                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"Replaying last song: `{nextTrack.Title}`.")
                };
            }

            return new CommandResponse
            {
                Embed = Utilities.CreateInfoEmbed($"Added last played track `{lastTrack?.Title}` to the queue.")
            };
        }

        public async Task<CommandResponse> ForceSkipCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker)
        {
            TryGetGuildConnection(client, guild, out var conn);

            if (!IsTrackLoaded(conn))
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"There is nothing to skip.")
                };
            }

            var title = conn.CurrentState.CurrentTrack.Title;

            await conn.SeekAsync(conn.CurrentState.CurrentTrack.Length);

            return new CommandResponse
            {
                Embed = Utilities.CreateTitleEmbed($"⏩ Skipped: `{title}`", DiscordColor.IndianRed, conn.CurrentState.CurrentTrack.Uri.ToString())
            };
        }

        public async Task<CommandResponse> VolumeCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker, int? volume)
        {
            TryGetGuildConnection(client, guild, out var conn);

            var eqsettings = EqualizerManager.GetOrCreateEqualizerSettingsForGuild(guild);

            if(!volume.HasValue || conn == null)
            {
                return new CommandResponse
                {
                    Embed = Utilities.CreateInfoEmbed($"Current Volume is at **{eqsettings.Volume}**! 🔊")
                };
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
            var queue = MusicQueueManager.GetOrCreateQueueForGuild(guild);

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

            MusicQueueManager.GetOrCreateQueueForGuild(guild).Mode = queueMode;

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
                return CommandResponse.Empty;
            }

            var queue = MusicQueueManager.GetOrCreateQueueForGuild(guild);

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
                responseWrapper.SetResponse(new CommandResponse
                {
                    Embed = Utilities.CreateErrorEmbed("I am not connected to a voice channel.")
                });
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

            if (conn.Channel == member.VoiceState?.Channel)
            {
                return true;
            }

            return false;
        }
        #endregion util
    }
}
