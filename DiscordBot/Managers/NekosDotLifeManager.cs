using DiscordBot.Attributes;
using Nekos.Net;
using Nekos.Net.Endpoints;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class NekosDotLifeManager
    {
        public SfwEndpoint GlobalV2SfwEndpointFallback { get; set; } = SfwEndpoint.Poke;
        public NsfwEndpoint GlobalV2NsfwEndpointFallback { get; set; } = NsfwEndpoint.Classic;

        public NekosDotLifeManager()
        {
            
        }

        /*public async Task<string> GetV2SfwAsync(string stringEndpoint, string fallback = null)
        {
            SfwEndpoint endpoint;
            try
            {
                endpoint = (SfwEndpoint) Enum.Parse(typeof(SfwEndpoint), stringEndpoint);
            }
            catch(Exception)
            {
                if(string.IsNullOrEmpty(fallback))
                {
                    // Use Global Fallback;
                    endpoint = GlobalV2SfwEndpointFallback;
                }
                else
                {
                    try
                    {
                        endpoint = (SfwEndpoint) Enum.Parse(typeof(SfwEndpoint), fallback);
                    }
                    catch (Exception)
                    {
                        // Guild fallback failed, using Global instead
                        endpoint = GlobalV2SfwEndpointFallback;
                        Log.Logger.Warning($"Guild provided fallback '{fallback}' doesn't exist, using global fallback '{endpoint}' instead.");
                    }
                }
            }

            return await GetV2SfwAsync(endpoint);
        }*/

        public async Task<string> GetAsync<ET>(string stringEndpoint, string fallback = null) where ET : Enum
        {
            ET endpoint = default(ET);
            bool useGlobalFallback = false;
            try
            {
                endpoint = (ET) Enum.Parse(typeof(ET), stringEndpoint, true);
            }
            catch (Exception)
            {
                Log.Logger.Information("Initial endpoint parse failed, falling back...");
                if (string.IsNullOrEmpty(fallback))
                {
                    // Use Global Fallback;
                    useGlobalFallback = true;
                }
                else
                {
                    try
                    {
                        endpoint = (ET) Enum.Parse(typeof(ET), fallback, true);
                    }
                    catch (Exception)
                    {
                        // provided fallback failed, using Global instead
                        useGlobalFallback = true;
                        Log.Logger.Warning($"Provided fallback '{fallback}' doesn't exist, using global fallback instead.");
                    }
                }
            }

            Log.Logger.Information($"[Nekos.Life] Endpoint:{endpoint}, useGlobalFallback:{useGlobalFallback}");

            return await GetAsync(endpoint, useGlobalFallback);
        }

        private async Task<string> GetAsync<ET>(ET genericEndpoint, bool useGlobal) where ET : Enum
        {
            switch(genericEndpoint)
            {
                case SfwEndpoint endpoint:
                    return await GetV2SfwAsync(useGlobal ? GlobalV2SfwEndpointFallback : endpoint);
                case NsfwEndpoint endpoint:
                    return await GetV2NsfwAsync(useGlobal ? GlobalV2NsfwEndpointFallback : endpoint);
                default:
                    throw new ArgumentException($"Invalid type or endpoint '{typeof(ET).Name}', '{genericEndpoint}'.");
            }
        }

        public string[] GetAllEnpoints<ET>()
        {
            return Enum.GetNames(typeof(ET));
        }

        private async Task<string> GetV2SfwAsync(SfwEndpoint endpoint)
        {
            return (await NekosClient.GetSfwAsync(endpoint)).FileUrl;
        }

        private async Task<string> GetV2NsfwAsync(NsfwEndpoint endpoint)
        {
            return (await NekosClient.GetNsfwAsync(endpoint)).FileUrl;
        }

    }
}
