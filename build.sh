dotnet publish -c Release --framework netcoreapp3.1

cd ./DiscordBot/
docker build -t discord-bot-image -f Dockerfile .

docker create --name discord-bot discord-bot-image