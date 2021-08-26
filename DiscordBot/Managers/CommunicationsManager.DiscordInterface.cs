using DiscordBotPluginBase.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    public partial class CommunicationsManager
    {

        public class DiscordInterface : IDiscordInterface
        {
            private DiscordClient _client;
            private DiscordGuild _guild;
            private DiscordChannel _channel;

            public DiscordInterface(DiscordClient client, ulong guildId, ulong channelId)
            {
                _client = client;

                Setup(guildId, channelId);
            }

            private async void Setup(ulong guildId, ulong channelId)
            {
                _guild = await _client.GetGuildAsync(guildId);
                _channel = await _client.GetChannelAsync(channelId);
            }

            public void SendEmbedMessage(string message, int r, int g, int b, string authorUrl = null, string authorText = null, string authorLink = null)
            {
                if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(authorText)) throw new ArgumentException("Either message or authorText have to contain characters.");

                _ = Task.Run(async () => {

                    var embedBuilder = new DiscordEmbedBuilder().WithColor(new DiscordColor(r / 255f, g / 255f, b / 255f));

                    if (!string.IsNullOrEmpty(message))
                        embedBuilder.WithTitle(message);

                    if (!string.IsNullOrEmpty(authorUrl))
                        embedBuilder.WithAuthor(authorText, authorLink, authorUrl);

                    var msg = await new DiscordMessageBuilder()
                        .AddEmbed(embedBuilder.Build())
                        .SendAsync(_channel);
                });
            }

            public void SendMessage(string message)
            {
                if (string.IsNullOrEmpty(message)) throw new ArgumentException("Argument may not be null or empty!");

                _ = Task.Run(async () => {
                    await _channel.SendMessageAsync(message);
                });
            }
        }

    }
}
