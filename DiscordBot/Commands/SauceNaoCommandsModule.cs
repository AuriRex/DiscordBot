using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Serilog;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    public class SauceNaoCommandsModule : BaseCommandModule
    {
        public SauceNaoManager SauceManager { private get; set; }

        [Command("sauce")]
        [Aliases("source")]
        [Description("Tries to get the source of an (usually NSFW) image.")]
        [RequireNsfw]
        [Cooldown(6, 30, CooldownBucketType.Global)]
        public async Task SauceCommand(CommandContext ctx, [Description("The URL of an image with unknown sauce.")] string imageUrl)
        {
            await ctx.TriggerTypingAsync();

            imageUrl = imageUrl.Replace("<", "");
            imageUrl = imageUrl.Replace(">", "");

            _ = Task.Run(async () => {
                Log.Logger.Information($"querrying SauceNao with [{imageUrl}]");

                var sauce = await SauceManager.GetSauceAsync(imageUrl);

                Log.Logger.Information($"Sauce aqquired: [{sauce}] -> {sauce?.Results?[0]?.SourceURL} - {sauce?.Results?[0]?.InnerSource}");


                if (sauce == null)
                {
                    Log.Logger.Warning("SauceNao, result is null!");
                    await ctx.RespondAsync($"Something went wrong with your request {ctx.User.Mention}. :(");
                    return;
                }

                var result = sauce.Results[0];

                if (result == null)
                {
                    Log.Logger.Warning("SauceNao, first result is null!");
                    await ctx.RespondAsync($"Something went wrong with your request {ctx.User.Mention}. ;-;");
                    return;
                }

                var embed = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithTitle(result.Name)
                    .WithImageUrl(result.ThumbnailURL)
                    .WithUrl(result.SourceURL)
                    .AddField("Similarity", result.Similarity, true)
                    .AddField("Database", result.DatabaseName, true);

                if (!string.IsNullOrEmpty(result.InnerSource))
                    embed.AddField("ALT-Source", result.InnerSource);

                var msg = await new DiscordMessageBuilder()
                .AddEmbed(embed.Build())
                .SendAsync(ctx.Channel);
            });
        }


    }
}
