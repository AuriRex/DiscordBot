using DiscordBot.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Extensions
{
    public static class UniversalCommandCallbackArgsExtensions
    {

        public static void SendResponse(this Action<UniversalCommandCallbackArgs> callback, Func<UniversalCommandCallbackArgs> response)
        {
            if (callback == null || response == null) return;

            callback.Invoke(response.Invoke());
        }

    }
}
