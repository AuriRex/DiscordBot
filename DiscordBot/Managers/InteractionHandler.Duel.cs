using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    public partial class InteractionHandler
    {

        public static DiscordMessageBuilder CreateDuelInteraction(DiscordUser duelInvoker, DiscordUser duelTargetUser, object duelGame = null)
        {
            var messageBuilder = new DiscordMessageBuilder();

            messageBuilder.Content = $"{duelInvoker.Username} has challanged {duelTargetUser.Mention} to a duel!\n{duelTargetUser.Username}, do you accept?";

            messageBuilder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, $"{CustomComponentIds.DUEL_ACCEPT_PREFIX}{duelTargetUser.Id}", "Accept", false),
                new DiscordButtonComponent(ButtonStyle.Danger, $"{CustomComponentIds.DUEL_DENY_PREFIX}{duelTargetUser.Id}", "Deny", false),
            });

            return messageBuilder;
        }

        public const string GAME_DATA_FIELD_NAME = "Game Data";
        public const string DUEL_DATA_FIELD_NAME = "Duel Data";

        private static async Task HandleDuelInteractions(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {
            var customId = eventArgs.Id;

            // duel_accept_<invokerUserId>_<userId>_<duelType>_<duelArgs?>
            // invokerUserId: user creating the duel
            // userId: target user, may be a non discord user id => any user
            // duelType: DiceRoll, TicTacToe, ...
            // duelArgs: arguments for the duel game; dice height (d6 or d20)

            // duel_deny_<userId>
            // userId: target user, may be a non discord user id => any user

            var duelDataJson = eventArgs.Message.Embeds[0].Fields.First(x => x.Name.Equals(DUEL_DATA_FIELD_NAME)).Value;

            duelDataJson = duelDataJson.Substring(2, duelDataJson.Length - 4);

            var duelData = Utilities.ParseJSON<Models.Duel.DuelData>(duelDataJson);

            if(customId.StartsWith(CustomComponentIds.DUEL_ACCEPT_PREFIX))
            {

                // Hand over to the respective duel game function if successful

                return;
            }

            if(customId.StartsWith(CustomComponentIds.DUEL_DENY_PREFIX))
            {
                try
                {
                    var denyTargetUserId = ulong.Parse(customId.Substring(CustomComponentIds.DUEL_DENY_PREFIX.Length));

                    if (eventArgs.User.Id == denyTargetUserId)
                    {
                        await DenyDuel(client, eventArgs);
                        return;
                    }
                }
                catch(Exception)
                {
                    await DenyDuel(client, eventArgs);
                }
                return;
            }

        }

        private static async Task DenyDuel(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {
            var response = new DiscordInteractionResponseBuilder();
            response.Content = "The duel request has been denied!";

            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, response);
        }
    }
}
