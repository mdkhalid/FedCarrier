# Logistics Management Platform - Design Document

## 1. Project Overview

### 1.1 Vision

FedCarrier is an enterprise-grade logistics management platform designed to
provide end-to-end visibility and control over shipment operations. The
platform serves three primary user roles — administrators, customers, and
drivers — through dedicated web and mobile interfaces.

### 1.2 Goals

- Centralize logistics operations across shipment lifecycle
- Provide real-time tracking and live status updates
- Enable automated billing and notification workflows
- Support horizontal scaling for high-volume throughput
- Maintain observability and auditability across all services

### 1.3 Target Users

| Role        | Interface        | Primary Actions                                      |
|-------------|------------------|------------------------------------------------------|
| Admin       | Angular Admin    | Manage users, services, rates, reports, configuration |
| Customer    | Customer Portal  | Place orders, track shipments, view invoices         |
| Driver      | Driver App       | Accept assignments, update status, navigate routes   |

## 2. Functional Modules

### 2.1 Identity
Authentication, authorization, user management, role-based access control,
JWT token issuance, refresh token rotation, password reset, MFA support.

### 2.2 Customer
Customer registration, profile management, address book, order placement,
order history, shipment inquiries.

### 2.3 Shipment
Shipment creation, status tracking, assignment to drivers, pickup/delivery
confirmation, shipment item management, status history.

### 2.4 Order
Order lifecycle management, order-to-shipment linkage, order status
transitions, order history and search.

### 2.5 Warehouse
Warehouse registration, inventory tracking, stock-in/stock-out operations,
warehouse-to-shipment handoff.

### 2.6 Fleet
Vehicle registration, fleet assignment, vehicle status tracking, maintenance
scheduling, capacity management.

### 2.7 Driver
Driver registration, profile management, current location updates, assignment
management, availability status, performance metrics.

### 2.8 Route Planning
Route optimization based on distance, traffic, vehicle capacity, delivery
windows, waypoint sequencing.

### 2.9 Tracking
Real-time shipment location tracking, GPS updates from driver app, estimated
delivery time calculation, tracking history.

### 2.10 Billing
Invoice generation, payment status tracking, rate card management,
invoice-to-shipment linkage, payment confirmation.

### 2.11 Notification
Email, SMS, and push notification dispatch, notification templates,
event-driven notification triggers, delivery status tracking.

### 2.12 Reporting
Shipment reports, financial reports, driver performance reports,
custom date-range reports, export to CSV/PDF.

## 3. Non-Functional Requirements

### 3.1 Availability
- Target uptime: 99.95%
- Stateless services for horizontal failover
- Health check endpoints on every service

### 3.2 Scalability
- Horizontal scaling per service based on load
- Database read replicas for query-heavy services
- Redis caching for frequently accessed data

### 3.3 Event-Driven Communication
- Asynchronous workflows via RabbitMQ
- Outbox pattern for reliable event publishing
- Saga pattern for distributed transactions

### 3.4 Observability
- Distributed tracing via OpenTelemetry
- Metrics collection via Prometheus
- Dashboards via Grafana
- Structured logging via Seq
- Correlation IDs propagated across services

### 3.5 Security
- JWT access tokens with short expiry (15 min)
- Refresh tokens with rotation
- HTTPS everywhere
- Role-based authorization at API and service level
- Secrets managed via environment variables or vault

### 3.6 CI/CD
- Automated build, test, and deploy pipelines
- Container image scanning
- Staging and production environments
- Blue-green or rolling deployments

## 4. Technology Stack

| Concern            | Technology                          | Justification                                      |
|--------------------|-------------------------------------|----------------------------------------------------|
| Backend Framework  | .NET 10                             | High performance, cross-platform, mature ecosystem |
| ORM                | EF Core                             | Strong LINQ support, migrations, SQL Server/Postgres|
| Frontend           | Angular                             | Type-safe, component-based, strong ecosystem       |
| API Gateway        | YARP                                | Lightweight, reverse proxy, route management       |
| Message Broker     | RabbitMQ                            | Reliable pub/sub, routing, dead-letter queues      |
| Cache              | Redis                               | Low-latency caching, pub/sub for invalidation      |
| Database           | SQL Server / PostgreSQL             | ACID compliance, mature tooling                    |
| Tracing            | OpenTelemetry                       | Vendor-neutral, integrates with all backends       |
| Metrics            | Prometheus                          | Industry standard, Grafana integration             |
| Dashboards         | Grafana                             | Rich visualization, alerting                       |
| Logging            | Seq                                 | Structured log search, .NET-native                 |
| Containerization   | Docker                              | Consistent dev/prod environments                   |
| Orchestration      | Kubernetes                          | Scaling, self-healing, declarative deployments     |
| Auth               | JWT + Refresh Tokens                | Stateless, scalable, standard claims               |
| Validation         | FluentValidation                    | Declarative, testable validation rules             |
| Mediation          | MediatR                             | CQRS pipeline, decoupled handlers                  |

## 5. Project Structure

```
FedCarrier/
├── src/
│   ├── gateways/
│   │   └── FedCarrier.Gateway/          # YARP API Gateway
│   ├── services/
│   │   ├── FedCarrier.Identity/         # Identity service
│   │   ├── FedCarrier.Customer/         # Customer service
│   │   ├── FedCarrier.Shipment/         # Shipment service
│   │   ├── FedCarrier.Order/            # Order service
│   │   ├── FedCarrier.Warehouse/        # Warehouse service
│   │   ├── FedCarrier.Fleet/            # Fleet service
│   │   ├── FedCarrier.Driver/           # Driver service
│   │   ├── FedCarrier.Routing/          # Route Planning service
│   │   ├── FedCarrier.Tracking/         # Tracking service
│   │   ├── FedCarrier.Billing/          # Billing service
│   │   ├── FedCarrier.Notification/     # Notification service
│   │   └── FedCarrier.Reporting/        # Reporting service
│   ├── clients/
│   │   ├── FedCarrier.Admin/            # Angular Admin
│   │   ├── FedCarrier.CustomerPortal/   # Angular Customer Portal
│   │   └── FedCarrier.DriverApp/        # Driver App (Angular/PWA)
│   └── shared/
│       ├── FedCarrier.Contracts/        # Shared DTOs and events
│       ├── FedCarrier.Common/           # Shared utilities and extensions
│       └── FedCarrier.Infrastructure/   # Shared infrastructure abstractions
├── docker/
├── k8s/
├── docs/
├── tests/
├── .github/                             # CI/CD workflows
├── docker-compose.yml
└── README.md
```

## 6. Architecture Principles

- **Clean Architecture**: Each service follows layered separation (API, Application, Domain, Infrastructure)
- **CQRS**: Commands and queries are separated for scalability
- **Event-Driven**: Services communicate via domain events through RabbitMQ
- **Database per Service**: Each service owns its data store
- **API-First**: Public contracts defined before implementation
- **Convention over Configuration**: Shared templates and generators reduce boilerplate