# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["JoFit.API/JoFit.API.csproj", "JoFit.API/"]
COPY ["JoFit.Application/JoFit.Application.csproj", "JoFit.Application/"]
COPY ["JoFit.Contracts/JoFit.Contracts.csproj", "JoFit.Contracts/"]
COPY ["JoFit.Domain/JoFit.Domain.csproj", "JoFit.Domain/"]
COPY ["JoFit.Infrastructure/JoFit.Infrastructure.csproj", "JoFit.Infrastructure/"]
RUN dotnet restore "./JoFit.API/JoFit.API.csproj"
COPY . .
WORKDIR "/src/JoFit.API"
RUN dotnet build "./JoFit.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JoFit.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JoFit.API.dll"]