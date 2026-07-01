# ---------- STAGE 1: BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiem doar csproj-ul întâi (cache eficient la restore)
COPY SyncretAPI/SyncretAPI.csproj SyncretAPI/
RUN dotnet restore SyncretAPI/SyncretAPI.csproj

# Copiem restul codului și publicăm
COPY SyncretAPI/ SyncretAPI/
RUN dotnet publish SyncretAPI/SyncretAPI.csproj -c Release -o /app/publish

# ---------- STAGE 2: RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# API-ul ascultă pe 8080 în container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SyncretAPI.dll"]