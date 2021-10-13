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

        private Dictionary<DiscordGuild, EQSettings> _eqSettingsForGuild;


    }

    public class EQSettings
    {
        private float[] _bandValues = new float[15];

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
    }
}
