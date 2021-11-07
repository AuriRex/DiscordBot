using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using System.Collections.Immutable;
using DiscordBot.Commands;

namespace DiscordBot.Managers
{
    public partial class InteractionHandler
    {
        internal static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {

            var customId = eventArgs.Interaction?.Data?.CustomId;

            if (string.IsNullOrEmpty(customId)) return;

            DateTimeOffset timestamp = eventArgs.Message.EditedTimestamp ?? eventArgs.Message.CreationTimestamp;
            if(timestamp.AddMinutes(5) < DateTimeOffset.UtcNow)
            {
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("This interaction has expired!"));
                return;
            }

            if (customId.StartsWith("eq_"))
            {
                await HandleEqualizerSettingsInteractions(client, eventArgs);
                return;
            }
            

            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("No more buttons for you >:)"));

            return;
        }

        public static class CustomComponentIds
        {
            public const string EQ_DROPDOWN_LOW_FRQZ = "eq_dd_low_frqz";
            public const string EQ_DROPDOWN_MID_FRQZ = "eq_dd_mid_frqz";
            public const string EQ_DROPDOWN_HIG_FRQZ = "eq_dd_hig_frqz";
            public const string EQ_INCREASE_VALUE_PREFIX = "eq_frqz_up_";
            public const string EQ_DECREASE_VALUE_PREFIX = "eq_frqz_dn_";
            public const string EQ_DROPDOWN = "eq_dropdown";
            public const string EQ_APPLY = "eq_apply";
            public const string EQ_CANCEL = "eq_cancel";
        }
    }
}
