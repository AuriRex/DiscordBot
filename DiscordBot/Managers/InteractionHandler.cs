using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Serilog;

namespace DiscordBot.Managers
{
    public class InteractionHandler
    {
        internal static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {

            var customId = eventArgs.Interaction?.Data?.CustomId;

            if (string.IsNullOrEmpty(customId)) return;

            if(customId.StartsWith("eq_"))
            {
                await HandleEqualizerSettingsInteractions(client, eventArgs);
                return;
            }
            

            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("No more buttons for you >:)"));

            return;
        }

        private static async Task HandleEqualizerSettingsInteractions(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {
            var customId = eventArgs.Interaction.Data.CustomId;
            var eqSettings = EqualizerManager.Instance.GetOrCreateEqualizerSettingsForGuild(eventArgs.Guild);
            EQOffset eqOffset;

            if(customId.Equals(CustomComponentIds.EQ_DROPDOWN))
            {
                switch (eventArgs.Interaction.Data.Values[0])
                {
                    case CustomComponentIds.EQ_DROPDOWN_HIG_FRQZ:
                        eqOffset = EQOffset.Highs;
                        break;
                    case CustomComponentIds.EQ_DROPDOWN_MID_FRQZ:
                        eqOffset = EQOffset.Mids;
                        break;
                    case CustomComponentIds.EQ_DROPDOWN_LOW_FRQZ:
                        eqOffset = EQOffset.Lows;
                        break;
                    default:
                        eqOffset = eqSettings.LastUsedOffset;
                        break;
                }
            }
            else
            {
                eqOffset = eqSettings.LastUsedOffset;
            }

            try
            {
                string prefix = string.Empty;
                int modificationValue = 0;
                if (customId.StartsWith(CustomComponentIds.EQ_INCREASE_VALUE_PREFIX))
                {
                    prefix = CustomComponentIds.EQ_INCREASE_VALUE_PREFIX;
                    modificationValue = 1;
                }

                if (customId.StartsWith(CustomComponentIds.EQ_DECREASE_VALUE_PREFIX))
                {
                    prefix = CustomComponentIds.EQ_DECREASE_VALUE_PREFIX;
                    modificationValue = -1;
                }

                if(!string.IsNullOrEmpty(prefix) && modificationValue != 0)
                {
                    int bandId = int.Parse(customId.Replace(prefix, string.Empty));

                    eqSettings.ModifyBand(bandId, modificationValue);
                }
            }
            catch(Exception ex)
            {
                Log.Error($"An error occured while trying to handle EQSettings Interactions: {ex.Message}");
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(BuildEQSettingsEmbed(eqSettings, eqOffset).Build()));
                return;
            }
            

            eqSettings.LastUsedOffset = eqOffset;
            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(BuildEQSettingsMessageWithComponents(eqSettings, eqOffset)));
        }

        public const string EQS_EMBED_SELECTED_GREEN = "🟩";
        public const string EQS_EMBED_SELECTED_PURPLE = "🟪";
        public const string EQS_EMBED_UNSELECTED_ZERO = "⬜";
        public const string EQS_EMBED_EMPTY = "◾";
        public const string EQS_EMBED_DOWN_YELLOW = "🟨";
        public const string EQS_EMBED_DOWN_ORANGE = "🟧";
        public const string EQS_EMBED_DOWN_RED = "🟥";
        public const string EQS_EMBED_UP_UNCOLLAPSED = "🟦";
        public const string EQS_EMBED_UP_1 = "1️⃣";
        public const string EQS_EMBED_UP_2 = "2️⃣";
        public const string EQS_EMBED_UP_3 = "3️⃣";
        public const string EQS_EMBED_UP_4 = "4️⃣";
        public const string EQS_EMBED_UP_5 = "5️⃣";
        public const string EQS_NEWLINE = "\n";

        public static DiscordEmbedBuilder BuildEQSettingsEmbed(EQSettings eqSettings, EQOffset eqOffset)
        {
            var embed = new DiscordEmbedBuilder();

            embed.WithTitle("Equalizer Settings");
            //embed.WithDescription("###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############");

            // POC
            //embed.WithDescription("◾5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n4️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n5️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n5️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾1️⃣◾◾◾\n" + "🟪🟩🟪🟩🟪⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜\n" + "🟨🟨🟨🟨🟨◾◾◾◾◾◾◾◾◾◾\n🟨🟨🟨🟨🟨◾◾◾◾◾◾◾◾◾◾\n🟧🟧🟧🟧🟧◾◾◾◾◾◾◾◾◾◾\n🟧🟧🟧🟧◾◾◾◾◾◾◾◾◾◾◾\n🟥🟥🟥◾◾◾◾◾◾◾◾◾◾◾◾\n");

            ConstructEQSettingsEmbedContent(embed, eqSettings, eqOffset);

            return embed;
        }

        public static DiscordMessageBuilder BuildEQSettingsMessageWithComponents(EQSettings eqSettings, EQOffset eqOffset)
        {
            var builder = new DiscordMessageBuilder();


            var embed = BuildEQSettingsEmbed(eqSettings, eqOffset);
            

            // Collapse every 5 down 🟦

            builder.WithEmbed(embed);

            // Create the options for the user to pick
            var options = new List<DiscordSelectComponentOption>()
            {
                new DiscordSelectComponentOption("Low Frequenzies", CustomComponentIds.EQ_DROPDOWN_LOW_FRQZ, "Bass!", eqOffset == EQOffset.Lows, new DiscordComponentEmoji("📢")),
                new DiscordSelectComponentOption("Mid Frequenzies", CustomComponentIds.EQ_DROPDOWN_MID_FRQZ, "Mids!", eqOffset == EQOffset.Mids, new DiscordComponentEmoji("🎸")),
                new DiscordSelectComponentOption("High Frequenzies", CustomComponentIds.EQ_DROPDOWN_HIG_FRQZ, "Highs!", eqOffset == EQOffset.Highs, new DiscordComponentEmoji("🪁"))
            };

            // Make the dropdown
            var dropdown = new DiscordSelectComponent(CustomComponentIds.EQ_DROPDOWN, null, options, false, 1, 1);


            builder.AddComponents(new DiscordComponent[]
            {
                dropdown
            });

            int offset = (int) eqOffset;

            

            builder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_up_{offset}", null, eqSettings.IsAtMax(offset), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_up_{offset+1}", null, eqSettings.IsAtMax(offset+1), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_up_{offset+2}", null, eqSettings.IsAtMax(offset+2), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_up_{offset+3}", null, eqSettings.IsAtMax(offset+3), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_up_{offset+4}", null, eqSettings.IsAtMax(offset+4), new DiscordComponentEmoji("⬆️"))
            });

            builder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_dn_{offset}", null, eqSettings.IsAtMin(offset), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_dn_{offset+1}", null, eqSettings.IsAtMin(offset+1), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_dn_{offset+2}", null, eqSettings.IsAtMin(offset+2), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_dn_{offset+3}", null, eqSettings.IsAtMin(offset+3), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_dn_{offset+4}", null, eqSettings.IsAtMin(offset+4), new DiscordComponentEmoji("⬇️"))
            });

            return builder;
        }

        private static void ConstructEQSettingsEmbedContent(DiscordEmbedBuilder embed, EQSettings eqSettings, EQOffset offset)
        {

            StringBuilder builder = new StringBuilder();

            var bands = eqSettings.GetBandsAsInts();

            if (bands.Any(b => b > 5))
            {
                // collapse every 5 lines
            }
            else
            {
                // Simple 5 Lines up + 1 neutral + 5 down
                for (int i = 0; i < 5; i++)
                {

                }
            }

            /*
        EQS_EMBED_SELECTED_GREEN = "🟩";
        EQS_EMBED_SELECTED_PURPLE = "🟪";
        EQS_EMBED_UNSELECTED_ZERO = "⬜";
            */

            BuildNeutralRow(builder, offset);

        /*
        EQS_EMBED_EMPTY = "◾";
        EQS_EMBED_DOWN_YELLOW = "🟨";
        EQS_EMBED_DOWN_ORANGE = "🟧";
        EQS_EMBED_DOWN_RED = "🟥";
        */

            if(bands.Any(b => b < 0))
            {
                for (int row = 0; row < 5; row++)
                {
                    for (int i = 0; i < bands.Length; i++)
                    {
                        if(bands[i] + row + 1 <= 0)
                            switch(bands[i])
                            {
                                case -1:
                                case -2:
                                    builder.Append(EQS_EMBED_DOWN_YELLOW);
                                    break;
                                case -3:
                                case -4:
                                    builder.Append(EQS_EMBED_DOWN_ORANGE);
                                    break;
                                case -5:
                                    builder.Append(EQS_EMBED_DOWN_RED);
                                    break;
                                default:
                                    builder.Append(EQS_EMBED_EMPTY);
                                    break;
                            }
                        else builder.Append(EQS_EMBED_EMPTY);
                    }
                    builder.Append(EQS_NEWLINE);
                }
            }
            else
            {
                // No negatives, Collapse to only one line to save space
                for(int i = 0; i < bands.Length; i++)
                {
                    builder.Append(EQS_EMBED_EMPTY);
                }
                builder.Append(EQS_NEWLINE);
            }

            embed.WithDescription(builder.ToString());
        }

        private static void BuildNeutralRow(StringBuilder builder, EQOffset offset)
        {
            switch (offset)
            {
                case EQOffset.Lows:
                    for (int i = 0; i < 5; i++)
                    {
                        if (i % 2 == 0)
                            builder.Append(EQS_EMBED_SELECTED_PURPLE);
                        else
                            builder.Append(EQS_EMBED_SELECTED_GREEN);
                    }
                    AppendXTimes(builder, 10, EQS_EMBED_UNSELECTED_ZERO);
                    break;
                case EQOffset.Mids:
                    AppendXTimes(builder, 5, EQS_EMBED_UNSELECTED_ZERO);
                    for (int i = 0; i < 5; i++)
                    {
                        if (i % 2 == 0)
                            builder.Append(EQS_EMBED_SELECTED_PURPLE);
                        else
                            builder.Append(EQS_EMBED_SELECTED_GREEN);
                    }
                    AppendXTimes(builder, 5, EQS_EMBED_UNSELECTED_ZERO);
                    break;
                case EQOffset.Highs:
                    AppendXTimes(builder, 10, EQS_EMBED_UNSELECTED_ZERO);
                    for (int i = 0; i < 5; i++)
                    {
                        if (i % 2 == 0)
                            builder.Append(EQS_EMBED_SELECTED_PURPLE);
                        else
                            builder.Append(EQS_EMBED_SELECTED_GREEN);
                    }
                    break;
            }
            builder.Append(EQS_NEWLINE);
        }

        private static void AppendXTimes(StringBuilder builder, int x, string toAppend)
        {
            if (x < 0) throw new ArgumentException($"Argument '{nameof(x)}' must be positive!");
            for (int i = 0; i < x; i++)
            {
                builder.Append(toAppend);
            }
        }

        private static string BuildEQSettingsUpwardsLevel()
        {

            return string.Empty;
        }

        public static class CustomComponentIds
        {
            public const string EQ_DROPDOWN_LOW_FRQZ = "eq_dd_low_frqz";
            public const string EQ_DROPDOWN_MID_FRQZ = "eq_dd_mid_frqz";
            public const string EQ_DROPDOWN_HIG_FRQZ = "eq_dd_hig_frqz";
            public const string EQ_INCREASE_VALUE_PREFIX = "eq_frqz_up_";
            public const string EQ_DECREASE_VALUE_PREFIX = "eq_frqz_dn_";
            public const string EQ_DROPDOWN = "eq_dropdown";
        }
    }
}
