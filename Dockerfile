FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /
EXPOSE 80
EXPOSE 443

COPY . ./
RUN cd BlockBase.Node && dotnet restore && dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /BlockBase.Node
COPY --from=build-env /BlockBase.Node/out .
ENTRYPOINT ["dotnet", "BlockBase.Node.dll"]
