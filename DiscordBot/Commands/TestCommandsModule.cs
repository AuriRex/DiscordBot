using DiscordBot.Managers;
using DiscordBot.Models.Database.Discord;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
/*    [Group("Other")]
    [Description("Testing related")]*/
    public class TestCommandsModule : BaseCommandModule
    {
        public DataBaseManager DBManager { private get; set; }

        [Command("test")]
        [Description("Does this work? :>")]
        [RequireOwner]
        public async Task TestCommand(CommandContext ctx)
        {
            DBManager.InsertOrUpdateDiscordObject<DiscordUser, DBDiscordUser>(ctx.User, DataBaseManager.DISCORDUSER_COLLECTION);

            var msg = await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithTitle("Test with description")
                    .Build())
                .SendAsync(ctx.Channel);
        }

        [Command("test")]
        [Description("Does this work? :>")]
        [RequireOwner]
        public async Task TestCommand(CommandContext ctx, DiscordMember mention)
        {
            DBManager.InsertOrUpdateDiscordObject<DiscordUser, DBDiscordUser>(mention, DataBaseManager.DISCORDUSER_COLLECTION);

            var msg = await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithTitle("Test with description")
                    .WithAuthor(mention.Username, null, mention.AvatarUrl)
                    .Build())
                .SendAsync(ctx.Channel);
        }

        [Command("test_all")]
        [Description("Does this work? :>")]
        [RequireOwner]
        public async Task TestAllCommand(CommandContext ctx)
        {
            //DBManager.InsertOrUpdateDiscordObject<DiscordUser, DBDiscordUser>(ctx.User, DataBaseManager.DISCORDUSER_COLLECTION);

            var all = DBManager.GetAllInCollection<DBDiscordUser>(DataBaseManager.DISCORDUSER_COLLECTION);

            string msg = string.Empty;
            foreach(DBDiscordUser user in all)
            {
                msg += user.Username + ", ";
            }
            msg = msg.Substring(0, msg.Length - 2);

            await ctx.RespondAsync($"{all?.Count}: {msg}");
        }

    }
}
