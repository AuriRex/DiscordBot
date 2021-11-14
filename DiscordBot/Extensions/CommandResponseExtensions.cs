using DiscordBot.Events;
using System;
using System.Collections.Generic;
using System.Text;
using static DiscordBot.Events.CommandResponse;

namespace DiscordBot.Extensions
{
    public static class CommandResponseExtensions
    {
        public static void SetResponse(this CommandResponseWrapper wrapper, CommandResponse response)
        {
            if (wrapper == null) return;
            wrapper.Response = response;
        } 

    }
}
