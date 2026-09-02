FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ConferenceRooms.Api/ConferenceRooms.Api.csproj ConferenceRooms.Api/
RUN dotnet restore ConferenceRooms.Api/ConferenceRooms.Api.csproj
COPY ConferenceRooms.Api/ ConferenceRooms.Api/
RUN dotnet publish ConferenceRooms.Api/ConferenceRooms.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ConferenceRooms.Api.dll"]
