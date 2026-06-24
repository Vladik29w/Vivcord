#Nuget
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore

WORKDIR /src

COPY Vivcord.Server.csproj ./

RUN dotnet restore

#Build
FROM restore AS build

# Copy all source code
COPY . .

RUN dotnet build -c Release --no-restore

#Publish
FROM build AS publish

RUN dotnet publish -c Release -o /app/publish --no-build

#Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Vivcord.Server.dll"]
