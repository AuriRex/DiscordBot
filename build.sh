docker build -t aurirex/polarisdiscordbot .
docker push aurirex/polarisdiscordbot

#dotnet build -c Release --framework netcoreapp3.1

#cd ./DiscordBot/
#docker build -t discord-bot-image -f Dockerfile .

#docker create --name discord-bot discord-bot-image