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

        [Command("jifify")]
        public async Task Jifify(CommandContext ctx, [RemainingText] string gif)
        {
            StringBuilder ret = new StringBuilder();
            for(int i = 0; i < gif.Length-1; i++)
            {
                char prev = i > 0 ? gif[i - 1] : ' ';
                char current = gif[i];
                char next = gif[i + 1];

                if (next == ' ')
                {
                    ret.Append(current);
                    continue;
                }
                
                switch (prev)
                {
                    case ' ': break;
                    case '.': break;
                    default:
                        ret.Append(current);
                        continue;
                }

                if(current == 'g')
                {
                    ret.Append('j');
                    continue;
                }

                if (current == 'G')
                {
                    ret.Append('J');
                    continue;
                }

                ret.Append(current);
            }

            await ctx.RespondAsync(ret.ToString());
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
