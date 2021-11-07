using DiscordBot.Commands;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Managers
{
    public partial class InteractionHandler
    {
        private static async Task HandleEqualizerSettingsInteractions(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {
            var customId = eventArgs.Interaction.Data.CustomId;
            var eqSettings = EqualizerManager.Instance.GetOrCreateEqualizerSettingsForGuild(eventArgs.Guild);
            EQOffset eqOffset;


            if (customId.Equals(CustomComponentIds.EQ_DROPDOWN))
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


            if (customId.Equals(CustomComponentIds.EQ_APPLY))
            {
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(BuildEQSettingsEmbed(eqSettings, eqOffset, EditingState.Saved).Build()));

                var member = (DiscordMember) eventArgs.Interaction.User;

                var conn = await LavaLinkCommandsModule.GetGuildConnection(client, member, null, false, member.VoiceState.Channel, null);

                await conn.AdjustEqualizerAsync(eqSettings.GetBands());

                return;
            }

            if (customId.Equals(CustomComponentIds.EQ_CANCEL))
            {
                eqSettings.RestoreFromLastAppliedSettings();

                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(BuildEQSettingsEmbed(eqSettings, eqOffset, EditingState.Canceled).Build()));

                return;
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

                if (!string.IsNullOrEmpty(prefix) && modificationValue != 0)
                {
                    int bandId = int.Parse(customId.Replace(prefix, string.Empty));

                    eqSettings.ModifyBand(bandId, modificationValue);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while trying to handle EQSettings Interactions: {ex.Message}");
                await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(BuildEQSettingsEmbed(eqSettings, eqOffset, EditingState.Canceled).Build()));
                return;
            }


            eqSettings.LastUsedOffset = eqOffset;
            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder(BuildEQSettingsMessageWithComponents(eqSettings, eqOffset, EditingState.Editing)));
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

        public enum EditingState
        {
            Canceled,
            Editing,
            Saved
        }

        public static DiscordEmbedBuilder BuildEQSettingsEmbed(EQSettings eqSettings, EQOffset eqOffset, EditingState editingState)
        {
            var embed = new DiscordEmbedBuilder();

            embed.WithTitle("Equalizer Settings");
            embed.WithAuthor($"Volume: {eqSettings.Volume}");
            //embed.WithDescription("###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############\n###############");

            // POC
            //embed.WithDescription("◾5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n4️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n5️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾◾◾◾◾\n5️⃣5️⃣5️⃣5️⃣5️⃣◾◾◾◾◾◾1️⃣◾◾◾\n" + "🟪🟩🟪🟩🟪⬜⬜⬜⬜⬜⬜⬜⬜⬜⬜\n" + "🟨🟨🟨🟨🟨◾◾◾◾◾◾◾◾◾◾\n🟨🟨🟨🟨🟨◾◾◾◾◾◾◾◾◾◾\n🟧🟧🟧🟧🟧◾◾◾◾◾◾◾◾◾◾\n🟧🟧🟧🟧◾◾◾◾◾◾◾◾◾◾◾\n🟥🟥🟥◾◾◾◾◾◾◾◾◾◾◾◾\n");

            ConstructEQSettingsEmbedContent(embed, eqSettings, eqOffset);

            embed.WithTimestamp(DateTime.Now);
            embed.WithFooter("Equalizer changes may take a few seconds to take effect!");

            switch (editingState)
            {
                case EditingState.Canceled:
                    embed.WithColor(DiscordColor.Gray);
                    break;
                case EditingState.Editing:
                    embed.WithColor(DiscordColor.Orange);
                    break;
                case EditingState.Saved:
                    embed.WithColor(DiscordColor.Green);
                    break;
            }


            return embed;
        }

        public static DiscordMessageBuilder BuildEQSettingsMessageWithComponents(EQSettings eqSettings, EQOffset eqOffset, EditingState editingState)
        {
            var builder = new DiscordMessageBuilder();


            var embed = BuildEQSettingsEmbed(eqSettings, eqOffset, editingState);


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
                new DiscordButtonComponent(ButtonStyle.Primary, $"{CustomComponentIds.EQ_INCREASE_VALUE_PREFIX}{offset}", null, eqSettings.IsAtMax(offset), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"{CustomComponentIds.EQ_INCREASE_VALUE_PREFIX}{offset+1}", null, eqSettings.IsAtMax(offset+1), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"{CustomComponentIds.EQ_INCREASE_VALUE_PREFIX}{offset+2}", null, eqSettings.IsAtMax(offset+2), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"{CustomComponentIds.EQ_INCREASE_VALUE_PREFIX}{offset+3}", null, eqSettings.IsAtMax(offset+3), new DiscordComponentEmoji("⬆️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"{CustomComponentIds.EQ_INCREASE_VALUE_PREFIX}{offset+4}", null, eqSettings.IsAtMax(offset+4), new DiscordComponentEmoji("⬆️"))
            });

            builder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Primary, $"{CustomComponentIds.EQ_DECREASE_VALUE_PREFIX}{offset}", null, eqSettings.IsAtMin(offset), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"{CustomComponentIds.EQ_DECREASE_VALUE_PREFIX}{offset+1}", null, eqSettings.IsAtMin(offset+1), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"{CustomComponentIds.EQ_DECREASE_VALUE_PREFIX}{offset+2}", null, eqSettings.IsAtMin(offset+2), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Success, $"{CustomComponentIds.EQ_DECREASE_VALUE_PREFIX}{offset+3}", null, eqSettings.IsAtMin(offset+3), new DiscordComponentEmoji("⬇️")),
                new DiscordButtonComponent(ButtonStyle.Primary, $"{CustomComponentIds.EQ_DECREASE_VALUE_PREFIX}{offset+4}", null, eqSettings.IsAtMin(offset+4), new DiscordComponentEmoji("⬇️"))
            });

            builder.AddComponents(new DiscordComponent[] {
                new DiscordButtonComponent(ButtonStyle.Success, CustomComponentIds.EQ_APPLY, "Apply", false),
                new DiscordButtonComponent(ButtonStyle.Danger, CustomComponentIds.EQ_CANCEL, "Cancel", false),
            });

            return builder;
        }

        private static readonly string[] _numbers = new string[]
        {
            EQS_EMBED_EMPTY,
            EQS_EMBED_UP_1,
            EQS_EMBED_UP_2,
            EQS_EMBED_UP_3,
            EQS_EMBED_UP_4,
            EQS_EMBED_UP_5
        };

        private static void ConstructEQSettingsEmbedContent(DiscordEmbedBuilder embed, EQSettings eqSettings, EQOffset offset)
        {

            StringBuilder builder = new StringBuilder();

            var bands = eqSettings.GetBandsAsInts();

            if (!bands.Any(b => b >= 20))
            {
                int toRemove = bands.Max() - (bands.Max() % 5);
                for (int row = 4; row > 0; row--)
                {
                    for (int i = 0; i < bands.Length; i++)
                    {
                        var val = Math.Max((bands[i] - toRemove) % 5, 0);
                        if (val - row >= 0)
                            builder.Append(EQS_EMBED_UP_UNCOLLAPSED);
                        else
                            builder.Append(EQS_EMBED_EMPTY);
                    }
                    builder.Append(EQS_NEWLINE);
                }
            }

            var upBuilders = new List<StringBuilder>();

            if (bands.Any(b => b >= 5))
            {
                // collapse every 5 lines
                while (bands.Any(b => b / 5 > 0))
                {
                    var upBuilder = new StringBuilder();
                    for (int i = 0; i < bands.Length; i++)
                    {
                        if (bands[i] <= 0)
                        {
                            upBuilder.Append(EQS_EMBED_EMPTY);
                            continue;
                        }
                        int collapse = bands[i] / 5;
                        int rest = bands[i] % 5;

                        if (collapse == 0)
                        {
                            upBuilder.Append(_numbers[Math.Max(rest, 0)]);
                        }
                        else
                        {
                            upBuilder.Append(EQS_EMBED_UP_5);
                        }


                        bands[i] = bands[i] - 5;
                    }
                    upBuilder.Append(EQS_NEWLINE);
                    upBuilders.Add(upBuilder);
                }

            }

            upBuilders.Reverse();

            foreach (var upb in upBuilders)
            {
                builder.Append(upb);
            }

            /*else
            {
                // Simple 5 Lines up + 1 neutral + 5 down
                for (int row = 5; row > 0; row--)
                {
                    for (int i = 0; i < bands.Length; i++)
                    {
                        if (bands[i] - row >= 0)
                            builder.Append(EQS_EMBED_UP_UNCOLLAPSED);
                        else
                            builder.Append(EQS_EMBED_EMPTY);
                    }
                    builder.Append(EQS_NEWLINE);
                }
            }*/

            bands = eqSettings.GetBandsAsInts();

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

            if (bands.Any(b => b < 0))
            {
                for (int row = 0; row < 5; row++)
                {
                    for (int i = 0; i < bands.Length; i++)
                    {
                        if (bands[i] + row + 1 <= 0)
                            switch (bands[i])
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
                for (int i = 0; i < bands.Length; i++)
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
    }
}
