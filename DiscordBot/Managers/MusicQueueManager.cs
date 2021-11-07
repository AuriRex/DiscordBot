using DiscordBot.Attributes;
using DiscordBot.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class MusicQueueManager
    {

        private Dictionary<DiscordGuild, MusicPlayerDataForGuild> _musicPlayerDataForGuild = new Dictionary<DiscordGuild, MusicPlayerDataForGuild>();

        public QueueForGuild GetOrCreateQueueForGuild(DiscordGuild guild)
        {
            var musicPlayerData = GetOrCreateMusicPlayerData(guild);
            return musicPlayerData?.Queue;
        }

        public MusicPlayerDataForGuild GetOrCreateMusicPlayerData(DiscordGuild guild)
        {
            if (guild == null) return null;

            if (_musicPlayerDataForGuild.TryGetValue(guild, out MusicPlayerDataForGuild mpd))
            {
                return mpd;
            }

            var newMusicPlayerData = new MusicPlayerDataForGuild(guild);
            _musicPlayerDataForGuild.Add(guild, newMusicPlayerData);
            return newMusicPlayerData;
        }

        /// <summary>
        /// Register all events here
        /// </summary>
        /// <param name="conn"></param>
        internal void OnConnected(LavalinkGuildConnection conn)
        {
            conn.PlaybackFinished -= OnPlaybackFinished;
            conn.PlaybackFinished += OnPlaybackFinished;
            conn.DiscordWebSocketClosed -= OnDiscordWebSocketException;
            conn.DiscordWebSocketClosed += OnDiscordWebSocketException;
            conn.TrackException -= OnTrackException;
            conn.TrackException += OnTrackException;
        }

        private async Task OnTrackException(LavalinkGuildConnection sender, TrackExceptionEventArgs e)
        {
            Log.Warning($"{nameof(OnTrackException)} called!: Error:{e.Error}, TrackTitle:{e.Track?.Title}");

            var musicPlayerData = GetOrCreateMusicPlayerData(sender.Guild);

            var channel = musicPlayerData.LastUsedPlayControlChannel;

            if (channel != null)
            {
                if(musicPlayerData.LastErrorMessage == null || (musicPlayerData.LastErrorMessage != null && musicPlayerData.LastErrorMessage.Timestamp.AddSeconds(5) < DateTimeOffset.UtcNow))
                {
                    musicPlayerData.LastErrorMessage = await channel.SendMessageAsync($"Playing of track '{e?.Track?.Title}' failed, {e.Error} ");
                }
                else
                {
                    musicPlayerData.LastErrorMessage = await musicPlayerData.LastErrorMessage.ModifyAsync($"{musicPlayerData.LastErrorMessage.Content}+");
                }
            }
        }

        private async Task OnDiscordWebSocketException(LavalinkGuildConnection sender, WebSocketCloseEventArgs e)
        {
            Log.Warning($"{nameof(OnDiscordWebSocketException)} called!: Code:{e.Code}, Reason:{e.Reason}, Remote:{e.Remote}");
        }

        public async Task OnPlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {
            if (sender.IsConnected)
            {
                var queue = GetOrCreateQueueForGuild(sender.Guild);

                LavalinkTrack nextTrack = null;
                if(e.Reason == TrackEndReason.LoadFailed)
                {
                    Log.Warning($"Playback of last track errored, retrying! ({queue.CurrentErrorRetryCount+1}/{QueueForGuild.MaxErrorRetry})");
                    nextTrack = queue.GetLastTrackLimited();
                    if (nextTrack == null)
                    {
                        Log.Warning($"Retring playback of Track '{queue.LastDequeuedTrack?.Title}' has failed too many times, skipping!");
                    }
                }
                
                if(nextTrack == null)
                {
                    nextTrack = queue.DequeueTrack();
                }

                if (nextTrack != null)
                {
                    Log.Information($"Playing '{nextTrack.Title}' in '{sender.Guild.Name}' / '{sender.Guild.Id}' from queue.");
                    await sender.PlayAsync(nextTrack);
                }
            }

            return;
        }

        public class MusicPlayerDataForGuild
        {
            public DiscordGuild Guild { get; private set; }
            private QueueForGuild _queue;
            public QueueForGuild Queue
            {
                get
                {
                    if(_queue == null)
                    {
                        _queue = new QueueForGuild(Guild);
                    }
                    return _queue;
                }
            }

            public DiscordChannel LastUsedPlayControlChannel { get; set; }
            public DiscordMessage LastErrorMessage { get; internal set; }

            public MusicPlayerDataForGuild(DiscordGuild guild)
            {
                Guild = guild;
            }
        }

        public class QueueForGuild
        {
            public static int MaxErrorRetry { get; set; } = 3;
            public int CurrentErrorRetryCount { get; private set; } = 0;
            public QueueMode Mode { get; set; }
            public DiscordGuild Guild => _attachedGuild;
            public LavalinkTrack LastDequeuedTrack { get; private set; }
            public TimeSpan LastDequeuedSongTime { get; private set; }
            public LavalinkTrack PeekTopTrack
            {
                get
                {
                    if (_tracks.Count == 0) return null;
                    return _tracks.Peek();
                }
            }

            public int Count => _tracks.Count;
            public bool IsRandomMode
            {
                get
                {
                    switch(Mode)
                    {
                        case QueueMode.Random:
                        case QueueMode.RandomLooping:
                            return true;
                    }
                    return false;
                }
            }

            public bool IsEmpty
            {
                get
                {
                    return _tracks.Count <= 0;
                }
            }

            private readonly DiscordGuild _attachedGuild;
            private readonly Queue<LavalinkTrack> _tracks;

            public QueueForGuild(DiscordGuild guild)
            {
                _attachedGuild = guild;
                _tracks = new Queue<LavalinkTrack>();
            }

            /// <summary>
            /// Get the last played Track again but returns null after calling this function <see cref="MaxErrorRetry"/> times without dequeueing a new track.
            /// </summary>
            /// <returns>The previously dequeued Track</returns>
            public LavalinkTrack GetLastTrackLimited()
            {
                if(CurrentErrorRetryCount >= MaxErrorRetry-1)
                {
                    CurrentErrorRetryCount = 0;
                    return null;
                }

                CurrentErrorRetryCount++;

                return LastDequeuedTrack;
            }

            /// <summary>
            /// Get the next Track to play<br/>
            /// The different queue modes are handled by this.
            /// Returns null if the queue is empty!
            /// </summary>
            /// <returns>The next Track</returns>
            public LavalinkTrack DequeueTrack()
            {
                LavalinkTrack track;

                switch (Mode)
                {
                    case QueueMode.Looping:
                        if (_tracks.Count == 0)
                            return LastDequeuedTrack;
                        track = _tracks.Dequeue();
                        _tracks.Enqueue(track);
                        break;
                    case QueueMode.RandomLooping:
                        if (_tracks.Count == 0)
                            return LastDequeuedTrack;
                        Shuffle();
                        track = _tracks.Dequeue();
                        _tracks.Enqueue(track);
                        break;
                    case QueueMode.Random:
                        if (_tracks.Count == 0)
                            return null;
                        Shuffle();
                        track = _tracks.Dequeue();
                        break;
                    default:
                    case QueueMode.Default:
                        if (_tracks.Count == 0)
                            return null;
                        track = _tracks.Dequeue();
                        break;
                }

                LastDequeuedTrack = track;

                CurrentErrorRetryCount = 0;

                return track;
            }

            /// <summary>
            /// Shuffles the queue
            /// </summary>
            public void Shuffle()
            {
                List<LavalinkTrack> tracks = new List<LavalinkTrack>(_tracks);

                tracks.Shuffle();

                _tracks.Clear();

                foreach(LavalinkTrack track in tracks)
                {
                    _tracks.Enqueue(track);
                }
            }

            /// <summary>
            /// Tries to get the top <paramref name="x"/> Tracks in the queue <br/>This does not guarantee that the returned List is going to contain <paramref name="x"/> number of Tracks!
            /// </summary>
            /// <param name="x">Max number of Tracks</param>
            /// <returns></returns>
            public List<LavalinkTrack> GetTopXTracks(int x)
            {
                var list = new List<LavalinkTrack>();
                if (x < 1) return list;
                foreach(LavalinkTrack track in _tracks)
                {
                    list.Add(track);
                    x--;
                    if (x <= 0) break;
                }
                return list;
            }

            /// <summary>
            /// Total play time of the queue.
            /// </summary>
            /// <returns></returns>
            public TimeSpan GetTotalPlayTime()
            {
                float totalPlayTime = 0f;
                foreach (LavalinkTrack track in _tracks)
                {
                    totalPlayTime += (float) track.Length.TotalSeconds;
                }
                return TimeSpan.FromSeconds(totalPlayTime);
            }

            /// <summary>
            /// Add a Track to the queue
            /// </summary>
            /// <param name="track"></param>
            /// <returns>Play time of the queue until this song plays</returns>
            public TimeSpan EnqueueTrack(LavalinkTrack track)
            {
                TimeSpan timeUntilPlay = GetTotalPlayTime();

                _tracks.Enqueue(track);

                return timeUntilPlay;
            }

            /// <summary>
            /// Add a multiple Tracks to the queue
            /// </summary>
            /// <param name="tracks"></param>
            /// <returns>Play time of the queue until the first song plays</returns>
            public TimeSpan EnqueueTracks(IEnumerable<LavalinkTrack> tracks)
            {
                TimeSpan timeUntilFirstTrack = GetTotalPlayTime();
                foreach(LavalinkTrack track in tracks)
                {
                    _tracks.Enqueue(track);
                }
                return timeUntilFirstTrack;
            }

            public void SaveLastDequeuedSongTime(TimeSpan position)
            {
                LastDequeuedSongTime = position;
            }

            /// <summary>
            /// Clears the queue and returns the number of songs that have been removed.
            /// </summary>
            /// <returns></returns>
            public int Clear()
            {
                var songs = _tracks.Count;

                _tracks.Clear();

                return songs;
            }
        }

        public enum QueueMode
        {
            [AttachedStringAttribute("▶️")]
            Default,
            [AttachedStringAttribute("🔁")]
            Looping,
            [AttachedStringAttribute("🎲")]
            Random,
            [AttachedStringAttribute("🔀")]
            RandomLooping
        }
    }
}
