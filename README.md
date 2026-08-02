# FedCarrier - Logistics Management Platform

## Overview

Enterprise-grade logistics platform built with .NET 10, Angular, and Microservices.

## Prerequisites

- .NET 10 SDK
- Docker Desktop
- Node.js 20+ (for Angular clients)
- Git

## Getting Started

### 1. Clone the repository

\\\ash
git clone <repo-url>
cd FedCarrier
\\\

### 2. Start infrastructure dependencies

\\\ash
docker-compose up -d
\\\

This starts:
- SQL Server (port 1433)
- RabbitMQ (port 5672, management on 15672)
- Redis (port 6379)
- Seq (port 5341)
- Prometheus (port 9090)
- Grafana (port 3000)

### 3. Build and run

\\\ash
dotnet restore
dotnet build
\\\

### 4. Run a specific service

\\\ash
cd src/services/FedCarrier.Identity
dotnet run
\\\

### 5. Run tests

\\\ash
dotnet test tests/FedCarrier.Tests/FedCarrier.Tests.csproj
\\\

## Project Structure

\\\
FedCarrier/
+-- src/
¦   +-- gateways/
¦   ¦   +-- FedCarrier.Gateway/          # YARP API Gateway
¦   +-- services/
¦   ¦   +-- FedCarrier.Identity/         # Identity service
¦   ¦   +-- FedCarrier.Customer/         # Customer service
¦   ¦   +-- FedCarrier.Shipment/         # Shipment service
¦   ¦   +-- FedCarrier.Order/            # Order service
¦   ¦   +-- FedCarrier.Warehouse/        # Warehouse service
¦   ¦   +-- FedCarrier.Fleet/            # Fleet service
¦   ¦   +-- FedCarrier.Driver/           # Driver service
¦   ¦   +-- FedCarrier.Routing/          # Route Planning service
¦   ¦   +-- FedCarrier.Tracking/         # Tracking service
¦   ¦   +-- FedCarrier.Billing/          # Billing service
¦   ¦   +-- FedCarrier.Notification/     # Notification service
¦   ¦   +-- FedCarrier.Reporting/        # Reporting service
¦   +-- clients/
¦   ¦   +-- FedCarrier.Admin/            # Angular Admin
¦   ¦   +-- FedCarrier.CustomerPortal/   # Angular Customer Portal
¦   ¦   +-- FedCarrier.DriverApp/        # Driver App (PWA)
¦   +-- shared/
¦       +-- FedCarrier.Contracts/        # Shared DTOs and events
¦       +-- FedCarrier.Common/           # Shared utilities
¦       +-- FedCarrier.Infrastructure/   # Shared infrastructure
+-- docker/
+-- k8s/
+-- tests/
+-- .github/
+-- docker-compose.yml
+-- README.md
\\\

## Service Ports

| Service              | Port |
|----------------------|------|
| Gateway              | 5000 |
| Identity             | 5001 |
| Customer             | 5002 |
| Shipment             | 5003 |
| Order                | 5004 |
| Warehouse            | 5005 |
| Fleet                | 5006 |
| Driver               | 5007 |
| Route Planning       | 5008 |
| Tracking             | 5009 |
| Billing              | 5010 |
| Notification         | 5011 |
| Reporting            | 5012 |

## Development Guidelines

- Each service follows Clean Architecture (API, Application, Domain, Infrastructure)
- CQRS pattern with MediatR for commands and queries
- FluentValidation for input validation
- Outbox pattern for reliable event publishing
- Structured logging with Serilog and Seq
- Correlation IDs propagated across all services
- Database per service (SQL Server)
- RabbitMQ for inter-service async communication

## Monitoring

- Seq: http://localhost:5341
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)
- RabbitMQ Management: http://localhost:15672 (fedcarrier/fedcarrier@dev123!)
