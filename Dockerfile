FROM mcr.microsoft.com/powershell AS git-info-env
WORKDIR /GitInfo

COPY . ./

# Get last git commit message and if there's any uncommited changes and make those values available to the build environment
RUN apt-get update
RUN apt-get install -y git

RUN pwsh -Command "\$command = git diff --stat; if([string]::IsNullOrWhitespace(\$command)) { \$result = 'false'; } else { \$result = 'true'; } [string]::Concat(\$result) | Out-File './DiscordBot/tmp/IsDirty.txt'"
RUN git log -1 --pretty=%B > ./DiscordBot/tmp/LastCommitMessage.txt

# RUN echo 'test' > ./DiscordBot/tmp/LastCommitMessage.txt
# RUN echo 'false' > ./DiscordBot/tmp/IsDirty.txt

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /BuildEnv

# nuget source for DShapPlus nightlies
# RUN dotnet nuget add source https://nuget.emzi0767.com/api/v3/index.json

COPY . ./
COPY --from=git-info-env /GitInfo/DiscordBot/tmp/LastCommitMessage.txt /BuildEnv/DiscordBot/tmp/LastCommitMessage.txt
COPY --from=git-info-env /GitInfo/DiscordBot/tmp/IsDirty.txt /BuildEnv/DiscordBot/tmp/IsDirty.txt
RUN cd ./DiscordBot/
RUN dotnet restore
RUN dotnet publish -c Release --framework netcoreapp3.1 -o ./out

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS final
WORKDIR /DiscordBot

ENV COMPlus_EnableDiagnostics=0

COPY --from=build-env /BuildEnv/out/ ./
COPY --from=build-env /BuildEnv/wait-for-it.sh ./wait-for-it.sh

VOLUME /DiscordBot/data
#ENTRYPOINT ["dotnet", "DiscordBot.dll"]