# 🚀 StockFlow API

StockFlow is a production-style inventory and order management system
built with ASP.NET Core, designed to demonstrate clean architecture,
robust API design, and real-world backend engineering practices.

------------------------------------------------------------------------

## 🧰 Tech Stack

-   **ASP.NET Core Web API**
-   **Entity Framework Core (EF Core)**
-   **SQL Server**
-   **JWT Authentication (Role-based)**
-   **Serilog (Structured Logging)**
-   **FluentValidation**
-   **xUnit (Unit & Integration Testing)**

------------------------------------------------------------------------

## ✨ Key Features

### 📦 Product Management

-   Create and retrieve products
-   SKU uniqueness validation
-   Clean DTO mapping and result handling

### 📊 Inventory Management

-   Track stock levels
-   Adjust inventory with business rules
-   Maintain stock consistency

### 🧾 Order Processing

-   Create and confirm orders
-   Idempotent order creation (prevents duplicate requests)
-   Stock validation before order confirmation

### 🔐 Authentication & Authorization

-   JWT-based authentication
-   Role-based access control (Admin / User)

------------------------------------------------------------------------

## 🧠 Engineering Highlights

### ✅ Clean Architecture

The project follows a layered architecture:

StockFlow.Api → Controllers / Middleware
StockFlow.Application → Business Logic (Services)
StockFlow.Domain → Core Entities
StockFlow.Infrastructure → EF Core / Database

-   Separation of concerns
-   Testable business logic
-   Scalable structure

------------------------------------------------------------------------

### ⚠️ Global Exception Handling

-   Centralized exception middleware
-   Consistent API error responses

Custom exceptions: - NotFoundException - ConflictException -
ValidationException

------------------------------------------------------------------------

### 🔄 Idempotency Support

-   Prevents duplicate order creation
-   Safe retry mechanism for API calls
-   Important for real-world distributed systems

------------------------------------------------------------------------

### 🧾 Logging & Observability

-   Structured logging with Serilog
-   Request tracing with Correlation ID
-   Improved debugging and traceability

------------------------------------------------------------------------

## 🧪 Testing

The project includes both unit tests and integration tests.

### 🔹 Unit Tests

-   Focus on business logic (e.g. ProductService)
-   Use EF Core InMemory database

### 🔹 Integration Tests

-   Test full API pipeline
-   Validate HTTP responses and middleware behavior

------------------------------------------------------------------------

## ▶️ Run Locally

### Build

dotnet build

### Run API

dotnet run --project StockFlow.Api

### Run tests

dotnet test

------------------------------------------------------------------------

## 🐳 Run with Docker

### Build Docker Image

docker build -t stockflow-api .

### Run Container

docker run -d -p 5000:80 
  -e ASPNETCORE_ENVIRONMENT=Docker 
  --name stockflow 
  stockflow-api

### API will be available at:

http://localhost:5000

------------------------------------------------------------------------

## 📌 API Example

POST /api/products

{ "sku": "IP-001", "name": "iPhone 13" }

------------------------------------------------------------------------

## 🎯 What This Project Demonstrates

-   Production-style API design
-   Clean architecture in .NET
-   Logging, validation, idempotency
-   Unit + integration testing
- Containerized application with Docker

------------------------------------------------------------------------

## 📈 Future Improvements

-   Cloud deployment (Azure)\
-   CI/CD pipeline\
