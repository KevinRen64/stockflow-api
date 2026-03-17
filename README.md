# StockFlow API

StockFlow is a production-style inventory and order management API built with ASP.NET Core.

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Clean Architecture
- Idempotency support
- Serilog logging

## Features

- Product management
- Inventory adjustment
- Order creation and confirmation
- Stock movement tracking
- JWT authentication
- Idempotent order creation

## Architecture

The project follows a layered architecture:

StockFlow.Api  
StockFlow.Application  
StockFlow.Domain  
StockFlow.Infrastructure

## Run Locally

```bash
dotnet build
dotnet run --project StockFlow.Api
