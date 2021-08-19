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
            _client = new SauceNETClient(Environment.GetEnvironmentVariable(EnvironmentVariableName));

            Log.Logger.Information($"{nameof(SauceNaoManager)} created.");
        }

        public async Task<SauceNET.Model.Sauce> GetSauceAsync(string imageUrl)
        {
            return await _client.GetSauceAsync(imageUrl);
        }

    }
}
