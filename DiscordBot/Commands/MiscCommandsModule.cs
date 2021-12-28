using DiscordBot.Managers;
using DiscordBot.Models.Configuration;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    public class MiscCommandsModule : BaseCommandModule
    {
        public ScriptManager ScriptManager { private get; set; }
        public Config BotConfig { private get; set; }
        public Random Random { private get; set; }


        public const string NOT_THE_BEST_MENTION_REGEX = "<@!\\d+>";
        private Regex _badMentionRegex;
        public Regex BadMentionRegex
        {
            get
            {
                if(_badMentionRegex == null)
                    _badMentionRegex = new Regex(NOT_THE_BEST_MENTION_REGEX);
                return _badMentionRegex;
            }
        }

        private List<string> _emojis = new List<string>
        {
            "😱",
            "😳",
            "😖",
            "😩"
        };

        [Command("show")]
        [Hidden]
        public async Task ShowMe(CommandContext ctx, string me, [RemainingText]string remainingText)
        {
            if (!BadMentionRegex.IsMatch(ctx.Prefix))
                return;

            if(string.IsNullOrEmpty(me))
            {
                await ShowMe(ctx);
                return;
            }

            if (me == "me" || me == "us") me = "you";

            var emoji = _emojis[Random.Next(0, _emojis.Count)];

            if(string.IsNullOrWhiteSpace(remainingText))
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(emoji));
                return;
            }
            remainingText = remainingText.Replace("`", "'");

            await ctx.RespondAsync($"I don't think I can show `{remainingText}` to {me}. {emoji}");
        }

        [Command("show")]
        [Hidden]
        public async Task ShowMe(CommandContext ctx)
        {
            if (!BadMentionRegex.IsMatch(ctx.Prefix))
                return;

            var emoji = Config.GetGuildEmojiOrFallback(ctx.Client, BotConfig.CustomReactionSettings.ShowMeCommandReactionId, "❓");

            await ctx.Message.CreateReactionAsync(emoji);
        }

        [Command("eval")]
        [RequireOwner]
        public async Task EvalCommand(CommandContext ctx, [RemainingText] string code)
        {
            if (!BotConfig.EnableEvalCommand) return;

            await ctx.TriggerTypingAsync();

            if(code.StartsWith("```cs"))
            {
                code = code.Substring(5);
                code = code.Substring(0, code.Length - 3);
            }

            _ = Task.Run(async () => {
                string result = string.Empty;

                ScriptManager.SetData(ctx);

                try
                {
                    result = await ScriptManager.Evaluate(code);
                    if(string.IsNullOrWhiteSpace(result))
                    {
                        result = "Execution completed.";
                    }
                }
                catch(Exception ex)
                {
                    result = $"{ex}: {ex.Message}\n{ex.StackTrace}";
                }

                await ctx.RespondAsync(result);
            });
        }
    }
}
