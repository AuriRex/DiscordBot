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

        private Dictionary<DiscordGuild, QueueForGuild> _queueForGuild = new Dictionary<DiscordGuild, QueueForGuild>();

        public QueueForGuild GetOrCreateQueueForGuild(DiscordGuild guild)
        {
            if(_queueForGuild.TryGetValue(guild, out QueueForGuild qfg))
            {
                return qfg;
            }

            var newQueue = new QueueForGuild(guild);
            _queueForGuild.Add(guild, newQueue);
            return newQueue;
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
                        Log.Warning($"Retring playback of Track '{queue.LastDequeuedTrack}' has failed too many times, skipping!");
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

        public class QueueForGuild
        {
            public static int MaxErrorRetry { get; set; } = 3;
            public int CurrentErrorRetryCount { get; private set; } = 0;
            public QueueMode Mode { get; set; }
            public DiscordGuild Guild => _attachedGuild;
            public LavalinkTrack LastDequeuedTrack { get; private set; }
            public LavalinkTrack PeekTopTrack
            {
                get
                {
                    if (_tracks.Count == 0) return null;
                    return _tracks.Peek();
                }
            }

            public int Count => _tracks.Count;

            private readonly DiscordGuild _attachedGuild;
            private Queue<LavalinkTrack> _tracks;
            public QueueForGuild(DiscordGuild guild)
            {
                _attachedGuild = guild;
                _tracks = new Queue<LavalinkTrack>();
            }

            public LavalinkTrack GetLastTrackLimited()
            {
                if(CurrentErrorRetryCount >= MaxErrorRetry)
                {
                    CurrentErrorRetryCount = 0;
                    return null;
                }

                CurrentErrorRetryCount++;

                return LastDequeuedTrack;
            }

            public LavalinkTrack DequeueTrack()
            {
                LavalinkTrack track;
                switch (Mode)
                {
                    case QueueMode.Looping:
                        track = _tracks.Dequeue();
                        _tracks.Enqueue(track);
                        break;
                    case QueueMode.RandomLooping:
                        Shuffle();
                        track = _tracks.Dequeue();
                        _tracks.Enqueue(track);
                        break;
                    case QueueMode.Random:
                        Shuffle();
                        track = _tracks.Dequeue();
                        break;
                    default:
                    case QueueMode.Default:
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
            /// Total play time of the queue or null if it's soonTM, a moment in time between now and the heatdeath of the universe.
            /// </summary>
            /// <returns></returns>
            public TimeSpan? GetTotalPlayTime()
            {
                switch (Mode)
                {
                    case QueueMode.Random:
                    case QueueMode.RandomLooping:
                        return null;
                    default:
                    case QueueMode.Default:
                    case QueueMode.Looping:
                        float totalPlayTime = 0f;
                        foreach (LavalinkTrack track in _tracks)
                        {
                            totalPlayTime += (float) track.Length.TotalSeconds;
                        }
                        return TimeSpan.FromSeconds(totalPlayTime);
                }
                
            }

            /// <summary>
            /// Add a Track to the queue
            /// </summary>
            /// <param name="track"></param>
            /// <param name="returnTimeUntilPlay"></param>
            /// <returns>Play time of the queue until this song plays<br/>null if it's going to play soonTM, a moment in time between now and the heatdeath of the universe</returns>
            public TimeSpan? EnqueueTrack(LavalinkTrack track, bool returnTimeUntilPlay = true)
            {
                TimeSpan? timeUntilPlay = null;
                if(returnTimeUntilPlay)
                {
                    timeUntilPlay = GetTotalPlayTime();
                }

                _tracks.Enqueue(track);

                return timeUntilPlay;
            }

            public TimeSpan? EnqueueTracks(IEnumerable<LavalinkTrack> tracks)
            {
                TimeSpan? timeUntilFirstTrack = null;
                foreach(LavalinkTrack track in tracks)
                {
                    if(timeUntilFirstTrack == null)
                        timeUntilFirstTrack = EnqueueTrack(track, true);
                    else
                        EnqueueTrack(track, false);
                }

                return timeUntilFirstTrack;
            }

        }

        public enum QueueMode
        {
            Default,
            Looping,
            Random,
            RandomLooping
        }

    }
}
