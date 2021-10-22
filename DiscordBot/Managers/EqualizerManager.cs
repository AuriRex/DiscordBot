using DiscordBot.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class EqualizerManager
    {
        // 🙃
        internal static EqualizerManager Instance { get; private set; }

        private readonly Dictionary<DiscordGuild, EQSettings> _eqSettingsForGuild = new Dictionary<DiscordGuild, EQSettings>();

        public EqualizerManager()
        {
            Instance = this;
        }

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

        public int Volume { get; set; } = 100;
        public EQOffset LastUsedOffset { get; internal set; } = EQOffset.Lows;

        public const int BAND_LENGTH = 15;
        public const float BAND_VALUE_MAX = 1f;
        public const float BAND_VALUE_MIN = -0.25f;

        private float[] _bandValues = new float[BAND_LENGTH];
        private readonly DiscordGuild _guild;
        private EQSettings _LastAppliedSettings;

        public EQSettings(DiscordGuild guild)
        {
            _guild = guild;
        }

        private EQSettings(DiscordGuild guild, EQSettings other) : this(guild)
        {
            SetBandsFromOther(other);
        }

        /// <summary>
        /// Set the band at <paramref name="band"/> to <paramref name="value"/><br/><paramref name="value"/> must be between <see cref="BAND_VALUE_MIN"/> and <see cref="BAND_VALUE_MAX"/> (inclusive).
        /// </summary>
        /// <param name="band"></param>
        /// <param name="value"></param>
        public void SetBand(int band, float value)
        {
            if (band < 0 || band >= BAND_LENGTH) throw new ArgumentException($"{nameof(band)} must be between 0 and {BAND_LENGTH - 1} (including)");

            if (value < .25f) value = .25f;
            if (value > 1f) value = 1f;

            _bandValues[band] = value;
        }

        /// <summary>
        /// Get the value of the band at <paramref name="band"/>.
        /// </summary>
        /// <param name="band"></param>
        /// <returns></returns>
        public float GetBandValue(int band)
        {
            if (band < 0 || band >= BAND_LENGTH) throw new ArgumentException($"{nameof(band)} must be between 0 and {BAND_LENGTH - 1} (including)");

            return _bandValues[band];
        }

        /// <summary>
        /// Returns the current band settings and saves a copy to restore with <see cref="RestoreFromLastAppliedSettings"/>.
        /// </summary>
        /// <returns></returns>
        public LavalinkBandAdjustment[] GetBands()
        {
            _LastAppliedSettings = new EQSettings(_guild, this);
            LavalinkBandAdjustment[] bands = new LavalinkBandAdjustment[_bandValues.Length];

            for (int i = 0; i < _bandValues.Length; i++)
            {
                bands[i] = new LavalinkBandAdjustment(i, _bandValues[i]);
            }

            return bands;
        }

        /// <summary>
        /// Restores the last applied settings or resets it all to zero if the is none to reestore.
        /// </summary>
        public void RestoreFromLastAppliedSettings()
        {
            if (_LastAppliedSettings == null)
            {
                _bandValues = new float[BAND_LENGTH];
                return;
            }

            SetBandsFromOther(_LastAppliedSettings);
        }

        /// <summary>
        /// Sets all band values on this one to the ones from another <see cref="EQSettings"/> object.
        /// </summary>
        /// <param name="other"></param>
        private void SetBandsFromOther(EQSettings other)
        {
            for (int i = 0; i < _bandValues.Length; i++)
            {
                _bandValues[i] = other._bandValues[i];
            }
        }

        /// <summary>
        /// Increase or decrease a specific band where 1 equals 0.05 units
        /// </summary>
        /// <param name="bandIndex"></param>
        /// <param name="modificationValue"></param>
        public void ModifyBand(int bandIndex, int modificationValue)
        {
            if (bandIndex < 0 || bandIndex >= BAND_LENGTH) throw new ArgumentException($"{nameof(bandIndex)} must be between 0 and {BAND_LENGTH - 1} (including)");

            float value = _bandValues[bandIndex];

            int intValue = (int) (value * 20);

            value = (intValue + modificationValue) / 20f;

            _bandValues[bandIndex] = Math.Clamp(value, BAND_VALUE_MIN, BAND_VALUE_MAX);
        }

        /// <summary>
        /// Get all bands as ints where 1 = 0.05 units.
        /// </summary>
        /// <returns></returns>
        public int[] GetBandsAsInts()
        {
            var bands = new int[_bandValues.Length];
            for (int i = 0; i < _bandValues.Length; i++)
            {
                bands[i] = (int) (_bandValues[i] * 20);
            }
            return bands;
        }

        /// <summary>
        /// Set all bands from ints where 1 = 0.05 units.
        /// </summary>
        /// <param name="bands"></param>
        public void SetBandsFromInts(int[] bands)
        {
            if (bands.Length != _bandValues.Length) throw new ArgumentException($"Must match array length of {BAND_LENGTH}");
            for (int i = 0; i < _bandValues.Length; i++)
            {
                _bandValues[i] = (float) (bands[i] / 20f);
            }
        }

        /// <summary>
        /// Checks if the band at <paramref name="bandIndex"/> is at <see cref="BAND_VALUE_MAX"/>.
        /// </summary>
        /// <param name="bandIndex"></param>
        /// <returns></returns>
        public bool IsAtMax(int bandIndex)
        {
            if (bandIndex < 0 || bandIndex >= BAND_LENGTH) throw new ArgumentException($"{nameof(bandIndex)} must be between 0 and {BAND_LENGTH - 1} (including)");

            return _bandValues[bandIndex] >= BAND_VALUE_MAX;
        }

        /// <summary>
        /// Checks if the band at <paramref name="bandIndex"/> is at <see cref="BAND_VALUE_MIN"/>.
        /// </summary>
        /// <param name="bandIndex"></param>
        /// <returns></returns>
        public bool IsAtMin(int bandIndex)
        {
            if (bandIndex < 0 || bandIndex >= BAND_LENGTH) throw new ArgumentException($"{nameof(bandIndex)} must be between 0 and {BAND_LENGTH - 1} (including)");

            return _bandValues[bandIndex] <= BAND_VALUE_MIN;
        }
    }
}
