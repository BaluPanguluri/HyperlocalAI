# Cloud / container deployment (also works locally with docker run)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY HyperlocalAI.sln ./
COPY src/ ./src/
RUN dotnet restore src/HyperlocalAI.Web/HyperlocalAI.Web.csproj
RUN dotnet publish src/HyperlocalAI.Web/HyperlocalAI.Web.csproj -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HyperlocalAI.Web.dll"]
