using DiscordBot.Attributes;
using DiscordBot.Commands.Core;
using DiscordBot.Events;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DiscordBot.Events.CommandResponse;

namespace DiscordBot.Managers
{
    [AutoDI.Singleton]
    public class ScriptManager
    {
        public static ScriptManager Instance { get; private set; }
        private Script<object> script;
        private ScriptState<object> state;

        public ScriptHelper Helper { get; private set; } = new ScriptHelper();

        public ScriptManager()
        {
            Instance = this;

            var options = ScriptOptions.Default.AddReferences(typeof(DiscordBot.Program).Assembly, typeof(int).Assembly, typeof(DSharpPlus.BaseDiscordClient).Assembly, typeof(CommandContext).Assembly)
                .WithImports("DiscordBot.Managers");

            script = CSharpScript.Create<object>("var ctx = ScriptManager.Instance.Helper;", options);

            state = script.RunAsync().Result;
        }

        private void a()
        {
            //((DiscordBot.Commands.Core.LavaLinkCommandsCore) ctx.Client.GetCommandsNext().Services.GetService(typeof(DiscordBot.Commands.Core.LavaLinkCommandsCore))).ConnectToVoice(ctx.Client, ctx.Client.GetChannelAsync(871002087692058629).Result);
        }

        public void SetData(CommandContext ctx)
        {
            Helper.SetDataFromCommandContext(ctx);
        }

        public async Task<string> Evaluate(string code)
        {
            try
            {
                state = await state.ContinueWithAsync(code);
            }
            catch(CompilationErrorException e)
            {
                return string.Join(Environment.NewLine, e.Diagnostics);
            }
            catch(Exception)
            {
                throw;
            }

            return state.ReturnValue?.ToString();
        }

        public class ScriptHelper
        {
            public DiscordClient Client { get; private set; }
            public DiscordChannel CurrentChannel { get; private set; }
            public DiscordGuild CurrentGuild { get; private set; }
            public DiscordMember CurrentMember { get; private set; }
            public DiscordUser CurrentUser { get; private set; }

            private CommandContext LastCommandContext { get; set; }

            private DiscordChannel SomeRandomChannel { get; set; }

            public void Test()
            {
                //Client.GetChannelAsync(id).Result.GetMessageAsync(mid).Result.DeleteAsync().Wait();
            }

            public void SetDataFromCommandContext(CommandContext ctx)
            {
                Client = ctx.Client;
                CurrentChannel = ctx.Channel;
                CurrentGuild = ctx.Guild;
                CurrentMember = ctx.Member;
                CurrentUser = ctx.User;

                if(SomeRandomChannel == null)
                {
                    SomeRandomChannel = Client.GetChannelAsync(917549673789685831).Result;
                }

                LastCommandContext = ctx;
            }

            public CommandResponse Join(ulong id)
            {
                var LavaLinkCommandsCore = LastCommandContext.Services.GetService(typeof(LavaLinkCommandsCore)) as LavaLinkCommandsCore;

                var channel = Client.GetChannelAsync(id).Result;

                var wrapper = new CommandResponseWrapper();
                LavaLinkCommandsCore.ConnectToVoice(Client, channel, wrapper).Wait();
                return wrapper.ResponseOrEmpty;
            }

            public void Leave(ulong id = 0)
            {
                if (CurrentMember == null) throw new Exception("Must be executed in a Guild!");
                if (!LavaLinkCommandsCore.IsBotConnected(Client, CurrentGuild))
                {
                    throw new Exception("Bot is not connected in this guild!");
                }

                LavaLinkCommandsCore.TryGetGuildConnection(Client, CurrentGuild, out var conn);

                conn.DisconnectAsync().Wait();
            }

            public void Play(ulong channelId, ulong memberId, string url)
            {

                var channel = Client.GetChannelAsync(channelId).Result;

                var member = channel.Guild.GetMemberAsync(memberId).Result;

                var lavaLinkCommandsCore = LastCommandContext.Services.GetService(typeof(LavaLinkCommandsCore)) as LavaLinkCommandsCore;

                lavaLinkCommandsCore.PlayCommand(Client, channel.Guild, SomeRandomChannel, member, url, true).Wait();
            }

            public T GetService<T>()
            {
                return (T) LastCommandContext.Services.GetService(typeof(T));
            }

            public void TheUsual()
            {
                if (CurrentMember == null) throw new Exception("Must be executed in a Guild!");

                var lavaLinkCommandsCore = LastCommandContext.Services.GetService(typeof(LavaLinkCommandsCore)) as LavaLinkCommandsCore;

                lavaLinkCommandsCore.PlayCommand(Client, CurrentGuild, SomeRandomChannel, CurrentMember, "https://www.youtube.com/watch?v=dQw4w9WgXcQ", true).Wait();
            }
        }

    }
}
