using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Serilog;
using System;
using System.Collections.Generic;
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

        [Command("anime")]
        [Description("Anime image or gif")]
        //[RequireNsfw]
        //[Cooldown(6, 30, CooldownBucketType.Global)]
        public async Task NekoCommand(CommandContext ctx, [Description("The endpoint to use")] string endpoint)
        {
            await ctx.TriggerTypingAsync();

            endpoint = endpoint.Replace("<", "");
            endpoint = endpoint.Replace(">", "");

            _ = Task.Run(async () => {
                Log.Logger.Information($"asking nekos.life for [{endpoint}]");

                var result = await NekosManager.GetAsync<Nekos.Net.Endpoints.SfwEndpoint>(endpoint, Nekos.Net.Endpoints.SfwEndpoint.Poke.ToString());

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
            var all = NekosManager.GetAllEnpoints<Nekos.Net.Endpoints.SfwEndpoint>();

            await NekoCommand(ctx, all[Random.Next(0, all.Length)]);
        }

        [Command("hentai")]
        [Description("NSFW hentai image or gif")]
        [RequireNsfw]
        //[Cooldown(6, 30, CooldownBucketType.Global)]
        public async Task LewdNekoCommand(CommandContext ctx, [Description("The NSFW endpoint to use")] string endpoint)
        {
            await ctx.TriggerTypingAsync();

            endpoint = endpoint.Replace("<", "");
            endpoint = endpoint.Replace(">", "");

            _ = Task.Run(async () => {
                Log.Logger.Information($"asking nekos.life for (NSFW) [{endpoint}]");

                var result = await NekosManager.GetAsync<Nekos.Net.Endpoints.NsfwEndpoint>(endpoint, Nekos.Net.Endpoints.NsfwEndpoint.Classic.ToString());

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
            var all = NekosManager.GetAllEnpoints<Nekos.Net.Endpoints.NsfwEndpoint>();

            await LewdNekoCommand(ctx, all[Random.Next(0, all.Length)]);
        }

        [Command("endpoints")]
        [Aliases("anime-endpoints")]
        [Description("Lists all the available endpoints")]
        public async Task EndpointsCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<Nekos.Net.Endpoints.SfwEndpoint>();

            string msg = string.Empty;
            foreach(string str in all)
            {
                msg += str + ", ";
            }
            msg = msg.Substring(0, msg.Length - 2);

            await ctx.RespondAsync(msg);
        }

        [Command("nsfw-endpoints")]
        [Aliases("hentai-endpoints")]
        [Description("Lists all the available NSFW / Hentai endpoints")]
        [RequireNsfw]
        public async Task NsfwEndpointsCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<Nekos.Net.Endpoints.NsfwEndpoint>();

            string msg = string.Empty;
            foreach (string str in all)
            {
                msg += str + ", ";
            }
            msg = msg.Substring(0, msg.Length - 2);

            await ctx.RespondAsync(msg);
        }
    }
}
