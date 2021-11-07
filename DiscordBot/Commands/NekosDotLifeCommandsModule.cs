using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nekos.Net.Endpoints;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
/*    [Group("Nekos.L")]
    [Description("Commands used to talk to the Nekos.Life API")]*/
    public class NekosDotLifeCommandsModule : BaseCommandModule
    {
        public NekosDotLifeManager NekosManager { private get; set; }
        public Random Random { private get; set; }

        private static string[] sfwExceptions = new string[] {
                SfwEndpoint.Lizard.ToString()
        };

        private static string[] nsfwExceptions = new string[] {
                NsfwEndpoint.NsfwAvatar.ToString(),
                NsfwEndpoint.NsfwNekoGif.ToString(),
                /* The next few characters have been put into quarantine, please stay away. */ NsfwEndpoint.Blowjob.ToString(), /* End of quarantine zone. */
                NsfwEndpoint.Lewd_K.ToString(),
                NsfwEndpoint.Lewd_Kemo.ToString()
        };

        [Command("anime")]
        [Description("Anime image or gif")]
        public async Task NekoCommand(CommandContext ctx, [Description("The endpoint to use")] string endpoint)
        {
            await ctx.TriggerTypingAsync();

            endpoint = endpoint.Replace("<", "");
            endpoint = endpoint.Replace(">", "");

            _ = Task.Run(async () => {
                Log.Logger.Information($"asking nekos.life for [{endpoint}]");

                var result = await NekosManager.GetAsync<SfwEndpoint>(endpoint, SfwEndpoint.Poke.ToString());

                var embed = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithImageUrl(result);

                var msg = await new DiscordMessageBuilder()
                .AddEmbed(embed.Build())
                .SendAsync(ctx.Channel);
            });
        }
        [Command("anime")]
        public async Task NekoCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<SfwEndpoint>().Except(sfwExceptions).ToArray();

            await NekoCommand(ctx, all[Random.Next(0, all.Length)]);
        }

        [Command("lizard")]
        [Description("LIZARD!!!")]
        public async Task Lizard(CommandContext ctx)
        {
            await NekoCommand(ctx, SfwEndpoint.Lizard.ToString());
        }

        [Command("hentai")]
        [Description("NSFW hentai image or gif")]
        [RequireNsfw]
        public async Task LewdNekoCommand(CommandContext ctx, [Description("The NSFW endpoint to use")] string endpoint)
        {
            await ctx.TriggerTypingAsync();

            endpoint = endpoint.Replace("<", "");
            endpoint = endpoint.Replace(">", "");

            _ = Task.Run(async () => {
                Log.Logger.Information($"asking nekos.life for (NSFW) [{endpoint}]");

                var result = await NekosManager.GetAsync<NsfwEndpoint>(endpoint, NsfwEndpoint.Classic.ToString());

                var embed = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithImageUrl(result);

                var msg = await new DiscordMessageBuilder()
                .AddEmbed(embed.Build())
                .SendAsync(ctx.Channel);
            });
        }

        [Command("hentai")]
        [RequireNsfw]
        public async Task LewdNekoCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<NsfwEndpoint>().Except(nsfwExceptions).ToArray();

            await LewdNekoCommand(ctx, all[Random.Next(0, all.Length)]);
        }

        [Command("endpoints")]
        [Aliases("anime-endpoints")]
        [Description("Lists all the available endpoints")]
        public async Task EndpointsCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<SfwEndpoint>(); // No endpoint exceptions here 'cause Lizards are dope :sunglasses:

            string msg = string.Empty;
            foreach(string str in all)
            {
                msg += str + ", ";
            }
            if(msg.Length > 1)
                msg = msg.Substring(0, msg.Length - 2);

            await ctx.RespondAsync(msg);
        }

        [Command("nsfw-endpoints")]
        [Aliases("hentai-endpoints")]
        [Description("Lists all the available NSFW / Hentai endpoints")]
        [RequireNsfw]
        public async Task NsfwEndpointsCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<NsfwEndpoint>().Except(nsfwExceptions);

            string msg = string.Empty;
            foreach (string str in all)
            {
                msg += str + ", ";
            }
            if(msg.Length > 1)
                msg = msg.Substring(0, msg.Length - 2);

            await ctx.RespondAsync(msg);
        }
    }
}
