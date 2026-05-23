FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WindroseServerDocker.slnx ./
COPY src/Windrose.StateWeb.Core/Windrose.StateWeb.Core.csproj src/Windrose.StateWeb.Core/
COPY src/Windrose.StateWeb/Windrose.StateWeb.csproj src/Windrose.StateWeb/
RUN dotnet restore src/Windrose.StateWeb/Windrose.StateWeb.csproj

COPY src/Windrose.StateWeb.Core/ src/Windrose.StateWeb.Core/
COPY src/Windrose.StateWeb/ src/Windrose.StateWeb/
RUN dotnet publish src/Windrose.StateWeb/Windrose.StateWeb.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8781
ENV DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
EXPOSE 8781
ENTRYPOINT ["dotnet", "Windrose.StateWeb.dll"]
