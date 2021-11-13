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
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Core
{
    [AutoDI.SingletonCreateAndInstall]
    public class LavaLinkCommandsCore
    {
        public MusicQueueManager MusicQueueManager { private get; set; }

        public async Task PlayCommand(DiscordClient client, DiscordGuild guild, DiscordChannel invokerMessageChannel, DiscordMember invoker, string searchOrUrl, bool autoConnect = true, Action<UniversalCommandCallbackArgs> callback = null)
        {
            if (string.IsNullOrWhiteSpace(searchOrUrl)) return;

            if (!TryGetGuildConnection(client, guild, out var conn, out var node))
            {
                if(!autoConnect)
                {
                    callback.SendResponse(() => new UniversalCommandCallbackArgs
                    {
                        Embed = Utilities.CreateErrorEmbed("I am not connected to a voice channel.")
                    });
                    return;
                }

                var channel = invoker.VoiceState?.Channel;

                conn = await ConnectToVoice(client, channel, callback);

                if(conn == null)
                {
                    return;
                }
            }

            var musicPlayerData = MusicQueueManager.GetOrCreateMusicPlayerData(guild);

            if(!musicPlayerData.AllowNonPresentMemberControl)
            {
                if(!IsMemberInMusicChannel(client, guild, invoker))
                {
                    callback.SendResponse(() => new UniversalCommandCallbackArgs
                    {
                        Embed = Utilities.CreateErrorEmbed("You have to be in the voice channel to control the bot.")
                    });
                    return;
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
                callback.SendResponse(() => new UniversalCommandCallbackArgs {
                    Embed = Utilities.CreateErrorEmbed($"Track search failed for {searchOrUrl}. ({finalLoadResult.Exception.Message})")
                });
                return;
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
                callback.SendResponse(() => new UniversalCommandCallbackArgs {
                    Embed = Utilities.CreateSuccessEmbed($"{extra}Added **{finalLoadResult.Tracks.Count() - 1}** songs from playlist `{playlistName}` to the queue! 🎵", searchOrUrl)
                });
                return;
            }


            if (IsTrackLoaded(conn))
            {
                var timeUntil = queue.EnqueueTrack(track) + conn.CurrentState.CurrentTrack.Length - conn.CurrentState.PlaybackPosition;

                callback.SendResponse(() => new UniversalCommandCallbackArgs
                {
                    Embed = Utilities.CreateSuccessEmbed($"Added `{track.Title}` to the queue! {(!queue.IsRandomMode ? $"Estimated time until playback: {Utilities.SpecialFormatTimeSpan(timeUntil)}" : string.Empty)} 🎵", track.Uri.ToString())
                });
                return;
            }

            queue.EnqueueTrack(track);

            await conn.PlayAsync(queue.DequeueTrack());

            callback.SendResponse(() => new UniversalCommandCallbackArgs
            {
                Embed = Utilities.CreateSuccessEmbed($"Now playing `{track.Title}`! 🎵", track.Uri.ToString())
            });
        }

        #region util
        public async Task<LavalinkGuildConnection> ConnectToVoice(DiscordClient client, DiscordChannel channelToJoin, Action<UniversalCommandCallbackArgs> callback = null)
        {
            if(channelToJoin == null)
            {
                callback.SendResponse(() => new UniversalCommandCallbackArgs
                {
                    Embed = Utilities.CreateErrorEmbed($"Unable to join your voice channel.")
                });
                return null;
            }

            if (!TryGetNode(client, out var node))
            {
                callback.SendResponse(() => new UniversalCommandCallbackArgs
                {
                    Embed = Utilities.CreateErrorEmbed("The Lavalink connection is not established.")
                });
                return null;
            }

            if (channelToJoin?.Type != ChannelType.Voice)
            {
                callback.SendResponse(() => new UniversalCommandCallbackArgs
                {
                    Embed = Utilities.CreateErrorEmbed($"\"{channelToJoin?.Name}\" is not a valid voice channel.")
                });
                return null;
            }

            var conn = await node.ConnectAsync(channelToJoin);

            MusicQueueManager.OnConnected(conn);

            return conn;
        }

        public async Task<LavalinkGuildConnection> JoinMemberVoice(DiscordClient client, DiscordMember member, bool memberIsInvoker = false, Action<UniversalCommandCallbackArgs> callback = null)
        {
            var channelToJoin = member?.VoiceState?.Channel;

            if(channelToJoin == null)
            {
                callback.SendResponse(() => new UniversalCommandCallbackArgs
                {
                    Embed = Utilities.CreateErrorEmbed(memberIsInvoker ? $"You don't seem to be in a voice channel." : $"{member.Username} doesn't seem to be in a voice channel.")
                });
                return null;
            }

            return await ConnectToVoice(client, channelToJoin, callback);
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
