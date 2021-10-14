using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace DiscordBot.Managers
{
    public class InteractionHandler
    {
        internal static async Task ComponentInteractionCreated(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {

            switch(eventArgs.Interaction?.Data?.CustomId)
            {
                case CustomComponentIds.EQ_DD_MID_FRQZ:

                    return;
            }

            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("No more buttons for you >:)"));

            return;
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

        public static DiscordMessageBuilder BuildEQSettingsMessageWithComponents(EQSettings eqSettings, EQOffset eqOffset)
        {
            var builder = new DiscordMessageBuilder();

            var embed = new DiscordEmbedBuilder();

            embed.WithTitle("Equalizer Settings");
            //embed.WithDescription("###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############");

            // POC
            //embed.WithDescription("◾5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n4️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n5️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n5️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾1️⃣◾◾◾\n" + "🟪🟩🟪🟩🟪⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜\n" + "🟨🟨🟨🟨🟨◾◾◾◾◾◾◾◾◾◾\n🟨🟨🟨🟨🟨◾◾◾◾◾◾◾◾◾◾\n🟧🟧🟧🟧🟧◾◾◾◾◾◾◾◾◾◾\n🟧🟧🟧🟧◾◾◾◾◾◾◾◾◾◾◾\n🟥🟥🟥◾◾◾◾◾◾◾◾◾◾◾◾\n");

            ConstructEQSettingsEmbedContent(embed, eqSettings, eqOffset);

            // Collapse every 5 down 🟦

            builder.WithEmbed(embed.Build());

            // Create the options for the user to pick
            var options = new List<DiscordSelectComponentOption>()
            {
                new DiscordSelectComponentOption("Low Frequenzies", "eq_dd_low_frqz", "Bass!", isDefault: true, emoji: new DiscordComponentEmoji("📢")),
                new DiscordSelectComponentOption("Mid Frequenzies", "eq_dd_mid_frqz", "Mids!", emoji: new DiscordComponentEmoji("🎸")),
                new DiscordSelectComponentOption("High Frequenzies", "eq_dd_high_frqz", "Highs!", emoji: new DiscordComponentEmoji("🪁"))
            };

            // Make the dropdown
            var dropdown = new DiscordSelectComponent("dropdown", null, options, false, 1, 1);


            builder.AddComponents(new DiscordComponent[]
            {
                dropdown
            });

            /*builder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, "1_top", "Blurple!"),
                new DiscordButtonComponent(ButtonStyle.Secondary, "2_top", "Grey!"),
                new DiscordButtonComponent(ButtonStyle.Success, "3_top", "Green!"),
                new DiscordButtonComponent(ButtonStyle.Danger, "4_top", "Red!"),
                new DiscordLinkButtonComponent("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "Link!")
            });*/

            int offset = (int) eqOffset;

            builder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_up_{offset}", null, false, new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_up_{offset+1}", null, false, new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_up_{offset+2}", null, false, new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_up_{offset+3}", null, false, new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_up_{offset+4}", null, false, new DiscordComponentEmoji("⬆️"))
            });

            builder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_dn_{offset}", null, false, new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_dn_{offset+1}", null, false, new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_dn_{offset+2}", null, false, new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"eq_frqz_dn_{offset+3}", null, false, new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"eq_frqz_dn_{offset+4}", null, false, new DiscordComponentEmoji("⬇️"))
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

            switch(offset)
            {
                case EQOffset.Lows:

                    break;
            }

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
                        switch(bands[i] + row)
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

            for (int i = 0; i < 5; i++)
            {
                // downwards
            }

            embed.WithDescription(builder.ToString());
        }

        private static string BuildEQSettingsUpwardsLevel()
        {

            return string.Empty;
        }

        public static class CustomComponentIds
        {
            public const string EQ_DD_LOW_FRQZ = "eq_dd_low_frqz";
            public const string EQ_DD_MID_FRQZ = "eq_dd_mid_frqz";
            public const string EQ_DD_HIG_FRQZ = "eq_dd_hig_frqz";
        }
    }
}
