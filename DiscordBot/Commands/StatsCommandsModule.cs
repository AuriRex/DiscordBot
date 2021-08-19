using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    public class StatsCommandsModule : BaseCommandModule
    {
        public GuildConfigManager Manager { private get; set; }

        [Command("stats")]
        [Description("WIP")]
        public async Task StatsCommand(CommandContext ctx)
        {
            await ctx.RespondAsync($"{ctx.Member?.DisplayName} -> todo command");

            

            //ctx.Channel.GetMessagesBeforeAsync();
        }

        [Command("guild-info")]
        [Aliases("ginfo", "guildinfo")]
        [Description("Show Guild info")]
        public async Task InfoCommand(CommandContext ctx)
        {
            var guild = ctx.Guild;
            var owner = ctx.Guild.Owner;

            var embed = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithAuthor($"{guild.Name} - Guild Information", null, guild.IconUrl)
                    .WithTimestamp(System.DateTimeOffset.Now)
                    .AddField("#Members", guild.MemberCount.ToString(), true)
                    .AddField("Owner", $"{owner.Username}#{owner.Discriminator}{(owner.DisplayName != owner.Username ? $" ({owner.DisplayName})" : string.Empty)}", true)
                    .AddField("Creation Time", guild.CreationTimestamp.ToString(), true);


            var msg = await new DiscordMessageBuilder()
                .AddEmbed(embed.Build())
                .SendAsync(ctx.Channel);
            
        }

    }
}
