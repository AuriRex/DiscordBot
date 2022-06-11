using DiscordBot.Attributes;
using Nekos.Net.V3;
using Nekos.Net.V3.Endpoints;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class NekosDotLifeManager
    {
        private NekosV3Client _nekosClient;

        public NekosDotLifeManager()
        {
            _nekosClient = new NekosV3Client(new SerilogLoggerProvider(Log.Logger).CreateLogger("Nekos"));
        }

        public bool TryParseEndpoint<ET>(string stringEndpoint, out ET endpoint)
        {
            stringEndpoint = stringEndpoint.Replace(' ', '_');
            try
            {
                endpoint = (ET) Enum.Parse(typeof(ET), stringEndpoint, true);
                return true;
            }
            catch (Exception)
            {
                endpoint = default(ET);
                return false;
            }
        }

        public async Task<string> GetImageUrlAsync<T>(T value) where T : Enum
        {
            switch(value)
            {
                case SfwGifEndpoint sg:
                    _nekosClient.WithSfwGifEndpoint(sg);
                    break;
                case SfwImgEndpoint si:
                    _nekosClient.WithSfwImgEndpoint(si);
                    break;
                default:
                    throw new ArgumentException($"Invalid type provided: \"{typeof(T).FullName}\".");
            }

            var response = await _nekosClient.GetAsync();

            return response?.Data?.Response?.Url;
        }

        public string[] GetAllEnpoints<ET>()
        {
            return Enum.GetNames(typeof(ET));
        }
    }
}
