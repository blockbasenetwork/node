FROM centos:latest AS base
WORKDIR /app
RUN rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm
RUN yum install -y dotnet-sdk-2.2 aspnetcore-runtime-2.2 dotnet-runtime-2.2
EXPOSE 5001
EXPOSE 444

FROM mcr.microsoft.com/dotnet/core/sdk:2.1-stretch AS build
WORKDIR /src
COPY . .
RUN dotnet restore "BlockBase.Node/BlockBase.Node.csproj"
COPY . .
WORKDIR /src/BlockBase.Node
RUN dotnet build "BlockBase.Node.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlockBase.Node.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app/publish
RUN useradd -rm -d /home/block -s /bin/bash -u 1000 block
EXPOSE 80
EXPOSE 5000
EXPOSE 5001
EXPOSE 4444
COPY --from=publish /app/publish .
RUN chown block:block -R *
USER block
RUN dotnet dev-certs https
ENTRYPOINT ["dotnet", "BlockBase.Node.dll", "--urls", "http://0.0.0.0:5001"]
