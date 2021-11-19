using DiscordBot.Events;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using static DiscordBot.Events.CommandResponse;

namespace DiscordBot.Extensions
{
    public static class CommandResponseExtensions
    {
        public static void SetResponse(this CommandResponseWrapper wrapper, CommandResponse response)
        {
            if (wrapper == null) return;
            wrapper.Response = response;
        }

        /// <summary>
        /// Respond to the <seealso cref="CommandResponse"/> <paramref name="ctx"/> either with a Reaction (Emoji) or an Embed/Text as fallback.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="ctx"></param>
        /// <returns><paramref name="true"/> on success</returns>
        public static async Task<bool> RespondWithReactionOrRestTo(this CommandResponse response, CommandContext ctx)
        {
            if (response.Reaction != null)
            {
                await ctx.Message.CreateReactionAsync(response.Reaction);
                return true;
            }

            return await response.RespondTo(ctx);
        }

        /// <summary>
        /// Respond to the <seealso cref="CommandResponse"/> <paramref name="ctx"/> with an Embed/Text.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="ctx"></param>
        /// <returns><paramref name="true"/> on success</returns>
        public static async Task<bool> RespondTo(this CommandResponse response, CommandContext ctx)
        {
            if (response.IsEmptyResponse) return false;

            await ctx.RespondAsync(response.GetMessageBuilder());
            return true;
        }

        /// <summary>
        /// Respond to a SlashCommands <seealso cref="BaseContext"/> <paramref name="ctx"/> by either deleting it if it's empty or editing the original message.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="ctx"></param>
        /// <returns><paramref name="true"/> if message has been edited only and has not been deleted</returns>
        public static async Task<bool> DeleteOrEdit(this CommandResponse response, BaseContext ctx)
        {
            if (response.IsEmptyResponse)
            {
                await ctx.DeleteResponseAsync();
                return false;
            }

            await ctx.EditResponseAsync(response.GetWebhookBuilder());
            return true;
        }
    }
}
