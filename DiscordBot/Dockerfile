FROM mcr.microsoft.com/dotnet/runtime:3.1
ENV COMPlus_EnableDiagnostics=0
COPY bin/Release/netcoreapp3.1/publish/ DiscordBot/
WORKDIR /DiscordBot
VOLUME /DiscordBot/data
ENTRYPOINT ["dotnet", "DiscordBot.dll"]