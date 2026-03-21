# ===== Build Stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj first 
COPY ["StockFlow.Api/StockFlow.Api.csproj", "StockFlow.Api/"]
COPY ["StockFlow.Application/StockFlow.Application.csproj", "StockFlow.Application/"]
COPY ["StockFlow.Domain/StockFlow.Domain.csproj", "StockFlow.Domain/"]
COPY ["StockFlow.Infrastructure/StockFlow.Infrastructure.csproj", "StockFlow.Infrastructure/"]

RUN dotnet restore "StockFlow.Api/StockFlow.Api.csproj"

# copy all files
COPY . .

WORKDIR "/src/StockFlow.Api"
RUN dotnet publish -c Release -o /app/publish

# ===== Runtime Stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "StockFlow.Api.dll"]