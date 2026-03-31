FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/AzureAI.Core/AzureAI.Core.csproj", "src/AzureAI.Core/"]
COPY ["src/AzureAI.Application/AzureAI.Application.csproj", "src/AzureAI.Application/"]
COPY ["src/AzureAI.Infrastructure/AzureAI.Infrastructure.csproj", "src/AzureAI.Infrastructure/"]
COPY ["src/AzureAI.Api/AzureAI.Api.csproj", "src/AzureAI.Api/"]

RUN dotnet restore "src/AzureAI.Api/AzureAI.Api.csproj"

COPY . .

RUN dotnet publish "src/AzureAI.Api/AzureAI.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AzureAI.Api.dll"]
