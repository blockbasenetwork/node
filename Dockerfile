FROM mcr.microsoft.com/dotnet/core/aspnet:2.1-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY BlockBase.Node/BlockBase.Node.csproj BlockBase.Node/
COPY BlockBase.Extensions/BlockBase.Extensions.csproj BlockBase.Extensions/
COPY BlockBase.Utils/BlockBase.Utils.csproj BlockBase.Utils/
COPY BlockBase.Runtime/BlockBase.Runtime.csproj BlockBase.Runtime/
COPY BlockBase.Domain/BlockBase.Domain.csproj BlockBase.Domain/
COPY BlockBase.Network/BlockBase.Network.csproj BlockBase.Network/
COPY Open.P2P/Open.P2P.csproj Open.P2P/
COPY BlockBase.DataPersistence/BlockBase.DataPersistence.csproj BlockBase.DataPersistence/
COPY BlockBase.DataProxy/BlockBase.DataProxy.csproj BlockBase.DataProxy/
RUN dotnet restore "BlockBase.Node/BlockBase.Node.csproj"
COPY . .
WORKDIR "/src/BlockBase.Node"
RUN dotnet build "BlockBase.Node.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlockBase.Node.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlockBase.Node.dll"]
