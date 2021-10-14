using DiscordBot.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class EqualizerManager
    {
        private readonly Dictionary<DiscordGuild, EQSettings> _eqSettingsForGuild = new Dictionary<DiscordGuild, EQSettings>();

        public EQSettings GetOrCreateEqualizerSettingsForGuild(DiscordGuild guild)
        {
            if (guild == null) return null;

            if (_eqSettingsForGuild.TryGetValue(guild, out EQSettings eqSettingsForGuild))
            {
                return eqSettingsForGuild;
            }

            var eqSettings = new EQSettings(guild);
            _eqSettingsForGuild.Add(guild, eqSettings);
            return eqSettings;
        }


    }

    public enum EQOffset
    {
        Lows = 0,
        Mids = 5,
        Highs = 10
    }

    public class EQSettings
    {
        public DiscordGuild Guild => _guild;

        public int Volume { get; set; }

        private float[] _bandValues = new float[15];
        private readonly DiscordGuild _guild;

        public EQSettings(DiscordGuild guild)
        {
            _guild = guild;
        }

        public void SetBand(int band, float value)
        {
            if (band < 0 || band > 14) throw new ArgumentException($"{nameof(band)} must be between 0 and 14 (including)");

            if (value < .25f) value = .25f;
            if (value > 1f) value = 1f;

            _bandValues[band] = value;
        }

        public float GetBandValue(int band)
        {
            if (band < 0 || band > 14) throw new ArgumentException($"{nameof(band)} must be between 0 and 14 (including)");

            return _bandValues[band];
        }

        public LavalinkBandAdjustment[] GetBands()
        {
            LavalinkBandAdjustment[] bands = new LavalinkBandAdjustment[_bandValues.Length];

            for (int i = 0; i < _bandValues.Length; i++)
            {
                bands[i] = new LavalinkBandAdjustment(i, _bandValues[i]);
            }

            return bands;
        }

        public int[] GetBandsAsInts()
        {
            var bands = new int[_bandValues.Length];
            for (int i = 0; i < _bandValues.Length; i++)
            {
                bands[i] = (int) (_bandValues[i] * 20);
            }
            return bands;
        }

        public void SetBandsFromInts(int[] bands)
        {
            for (int i = 0; i < _bandValues.Length; i++)
            {
                _bandValues[i] = (float) (bands[i] / 20f);
            }
        }
    }
}
