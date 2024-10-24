FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

EXPOSE 80 8080

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN dotnet tool install -g dotnet-t4
ENV PATH="/root/.dotnet/tools:${PATH}"
WORKDIR /src
COPY ["./Adastral.Cockatoo.WebApi/Adastral.Cockatoo.WebApi.csproj", "./"]
RUN dotnet restore "Adastral.Cockatoo.WebApi.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Adastral.Cockatoo.WebApi/Adastral.Cockatoo.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./Adastral.Cockatoo.WebApi/Adastral.Cockatoo.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Adastral.Cockatoo.WebApi.dll", "--urls=http://+:80"]