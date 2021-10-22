using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    public class StatsCommandsModule : BaseCommandModule
    {
        public GuildConfigManager Manager { private get; set; }

        [Command("stats")]
        [Description("WIP")]
        [RequireOwner]
        public async Task StatsCommand(CommandContext ctx)
        {
            await ctx.RespondAsync($"{ctx.Member?.DisplayName} -> todo command");



            //ctx.Channel.GetMessagesBeforeAsync();
        }

        private static DateTime? _buildDate = null;

        [Command("bot-version")]
        [Aliases("version")]
        public async Task Version(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithTitle(ThisAssembly.Git.RepositoryUrl)
                    .WithUrl(ThisAssembly.Git.RepositoryUrl);

            embed.AddField("Version", $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}", true);
            embed.AddField("Branch", $"{ThisAssembly.Git.Branch}", true);
            embed.AddField("Commit", $"{ThisAssembly.Git.Commit}", true);

            if (!string.IsNullOrEmpty(ThisAssembly.Git.Tag))
                embed.AddField("Tag", $"{ThisAssembly.Git.Tag}", true);

            embed.AddField("Local Changes", $"{ThisAssembly.Git.IsDirty}", true);

            // https://stackoverflow.com/questions/1600962/displaying-the-build-date
            if (!_buildDate.HasValue)
            {
                _buildDate = DateTime.Parse(Properties.Resources.BuildDate);
            }

            var lastCommitMessage = Properties.Resources.LastCommitMessage;

            lastCommitMessage = lastCommitMessage.Trim();

            if (!string.IsNullOrWhiteSpace(lastCommitMessage))
                embed.AddField("Commit Message", $"{lastCommitMessage}");

            embed.AddField("Full SHA Commit Hash", $"{ThisAssembly.Git.Sha}");

            var commitDate = DateTime.Parse(ThisAssembly.Git.CommitDate);

            embed.AddField("Commit Date (UTC)", $"{commitDate.ToString("F")}");

            embed.AddField("Build Date (UTC)", $"{_buildDate.Value.ToString("F")}");

            embed.WithFooter("Localized Commit Date");
            embed.WithTimestamp(commitDate);


            var msg = new DiscordMessageBuilder()
                .AddEmbed(embed.Build());

            await ctx.Channel.SendMessageAsync(msg);
        }

        [Command("guild-info")]
        [Aliases("ginfo", "guildinfo")]
        [Description("Show Guild info")]
        [RequireGuild]
        public async Task InfoCommand(CommandContext ctx)
        {
            var guild = ctx.Guild;
            var owner = ctx.Guild.Owner;

            var guildCreationTimeSpan = DateTimeOffset.Now - guild.CreationTimestamp;

            var embed = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithAuthor($"{guild.Name} - Guild Information", guild.IconUrl, guild.IconUrl)
                    .WithTimestamp(System.DateTimeOffset.Now)
                    .AddField("#Members", guild.MemberCount.ToString(), true)
                    .AddField("Owner", $"{owner.Username}#{owner.Discriminator}{(owner.DisplayName != owner.Username ? $" ({owner.DisplayName})" : string.Empty)}", true)
                    .AddField("Creation Time", $"{guild.CreationTimestamp.ToString("F")}\n{guildCreationTimeSpan.ToString($"d' Day{(guildCreationTimeSpan.Days > 1? "s" : string.Empty)} and 'hh':'mm':'ss' ago'")}", true);

            if (!string.IsNullOrEmpty(guild.BannerUrl))
                embed.WithThumbnail(guild.BannerUrl);

            var msg = new DiscordMessageBuilder()
                .AddEmbed(embed.Build());

            await ctx.Channel.SendMessageAsync(msg);
        }

    }
}
