﻿using DiscordBot.Managers;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nekos.Net.V3.Endpoints;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Commands
{
    [Group("Nekos.L")]
    [Description("Commands used to talk to the Nekos.Life API")]
    public class NekosDotLifeCommandsModule : BaseCommandModule
    {
        public NekosDotLifeManager NekosManager { private get; set; }
        public Random Random { private get; set; }

        #region RandomPickExceptions
        private static string[] imageExceptions = new string[] {
                SfwImgEndpoint.Kiminonawa.ToString(),
                SfwImgEndpoint.Holo_Avatar.ToString(),
                SfwImgEndpoint.Keta_Avatar.ToString(),
                SfwImgEndpoint.Neko_Avatars_Avatar.ToString(),
                SfwImgEndpoint.No_Tag_Avatar.ToString(),
        };
        private static string[] gifExceptions = new string[] {
                //SfwGifEndpoint.
        };
        #endregion RandomPickExceptions

        public async Task GetResponse<ET>(CommandContext ctx, string stringEndpoint) where ET : Enum
        {
            Log.Logger.Information($"asking nekos.life for ({typeof(ET).Name}) [{stringEndpoint}]");

            if (NekosManager.TryParseEndpoint<ET>(stringEndpoint, out var endpointEnum))
            {
                var result = await NekosManager.GetImageUrlAsync(endpointEnum);

                var embed = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Purple)
                    .WithImageUrl(result)
                    .WithFooter(endpointEnum.ToString());

                await new DiscordMessageBuilder()
                    .AddEmbed(embed.Build())
                    .SendAsync(ctx.Channel);

                return;
            }

            await new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Something went wrong.")
                    .Build())
                .SendAsync(ctx.Channel);
        }

        #region Image
        [Command("image")]
        [Aliases("i")]
        [Description("Image")]
        public async Task NekoDLImageCommand(CommandContext ctx, [Description("The endpoint to use")] string endpoint)
        {
            await ctx.TriggerTypingAsync();

            endpoint = endpoint.Replace("<", "");
            endpoint = endpoint.Replace(">", "");

            _ = Task.Run(async () => {
                await GetResponse<SfwImgEndpoint>(ctx, endpoint);
            });
        }

        [Command("image")]
        public async Task NekoDLImageCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<SfwImgEndpoint>().Except(imageExceptions).ToArray();

            await NekoDLImageCommand(ctx, all[Random.Next(0, all.Length)]);
        }

        [Command("lizard")]
        [Description("LIZARD!!!")]
        public async Task Lizard(CommandContext ctx)
        {
            await NekoDLImageCommand(ctx, SfwImgEndpoint.Lizard.ToString());
        }
        #endregion Image

        #region Gif
        [Command("gif")]
        [Aliases("g")]
        [Description("Gif")]
        public async Task NekoDLGifCommand(CommandContext ctx, [Description("The endpoint to use")] string endpoint)
        {
            await ctx.TriggerTypingAsync();

            endpoint = endpoint.Replace("<", "");
            endpoint = endpoint.Replace(">", "");

            _ = Task.Run(async () => {
                await GetResponse<SfwGifEndpoint>(ctx, endpoint);
            });
        }

        [Command("gif")]
        public async Task NekoDLGifCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<SfwGifEndpoint>().Except(gifExceptions).ToArray();

            await NekoDLGifCommand(ctx, all[Random.Next(0, all.Length)]);
        }
        #endregion Gif

        #region endpoints
        [Command("image-endpoints")]
        [Aliases("iep")]
        [Description("Lists all the available endpoints")]
        public async Task EndpointsCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<SfwImgEndpoint>();

            await ctx.RespondAsync(string.Join(", ", all));
        }

        [Command("gif-endpoints")]
        [Aliases("gep")]
        [Description("Lists all the available gif endpoints")]
        public async Task GifEndpointsCommand(CommandContext ctx)
        {
            var all = NekosManager.GetAllEnpoints<SfwGifEndpoint>();

            await ctx.RespondAsync(string.Join(", ", all));
        }
        #endregion endpoints
    }
}
