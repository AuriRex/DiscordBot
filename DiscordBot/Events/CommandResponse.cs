using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Events
{
    public class CommandResponse
    {
        public static CommandResponse Empty { get; } = new CommandResponse();

        public bool IsEmptyResponse {
            get
            {
                return string.IsNullOrWhiteSpace(Content) && Embed == null;
            }
        }

        public string Content { get; set; }
        public DiscordEmbedBuilder Embed { get; set; }
        public List<DiscordComponent[]> Components { get; set; }
        public DiscordEmoji Reaction { get; set; }

        public DiscordMessageBuilder GetMessageBuilder()
        {
            var builder = new DiscordMessageBuilder();

            if (!string.IsNullOrWhiteSpace(Content))
                builder.Content = Content;

            if (Embed != null)
                builder.AddEmbed(Embed);

            if(Components != null)
            {
                foreach(var components in Components)
                {
                    builder.AddComponents(components);
                }
            }

            return builder;
        }

        public DiscordInteractionResponseBuilder GetInteractionResponseBuilder()
        {
            return new DiscordInteractionResponseBuilder(GetMessageBuilder());
        }

        public DiscordWebhookBuilder GetWebhookBuilder()
        {
            var builder = new DiscordWebhookBuilder();

            if (!string.IsNullOrWhiteSpace(Content))
                builder.Content = Content;

            if (Embed != null)
                builder.AddEmbed(Embed);

            if (Components != null)
            {
                foreach (var components in Components)
                {
                    builder.AddComponents(components);
                }
            }

            return builder;
        }

        public class CommandResponseWrapper
        {
            public bool HasValue
            {
                get
                {
                    return Response != null;
                }
            }

            public CommandResponse ResponseOrEmpty
            {
                get
                {
                    if (HasValue) return Response;
                    return CommandResponse.Empty;
                }
            }

            public CommandResponse Response { get; set; }

        }
    }
}
