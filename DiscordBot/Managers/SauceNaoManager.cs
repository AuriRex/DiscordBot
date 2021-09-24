using DiscordBot.Attributes;
using SauceNET;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class SauceNaoManager
    {
        public static string EnvironmentVariableName { get; set; } = "saucenao_api";

        private SauceNETClient _client;

        public SauceNaoManager()
        {
            string apiKey = Environment.GetEnvironmentVariable(EnvironmentVariableName);

            if(string.IsNullOrWhiteSpace(apiKey))
            {
                Log.Logger.Warning($"{nameof(SauceNaoManager)} creation failed, no API Key provided!");
                return;
            }

            _client = new SauceNETClient(apiKey);

            Log.Logger.Information($"{nameof(SauceNaoManager)} created.");
        }

        public async Task<SauceNET.Model.Sauce> GetSauceAsync(string imageUrl)
        {
            if (_client == null) throw new Exception("No Client available. (API key missing?)");
            return await _client.GetSauceAsync(imageUrl);
        }

    }
}
