FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY IronMindGym/IronMindGym.csproj IronMindGym/packages.lock.json IronMindGym/
RUN dotnet restore IronMindGym/IronMindGym.csproj --locked-mode
COPY IronMindGym/ IronMindGym/
RUN dotnet publish IronMindGym/IronMindGym.csproj -c Release --no-restore -o /publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish .
RUN mkdir -p /app/keys && chown -R $APP_UID /app/keys
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "IronMindGym.dll"]
