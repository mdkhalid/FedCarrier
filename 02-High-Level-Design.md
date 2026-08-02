# High-Level Design - FedCarrier

## 1. System Architecture

FedCarrier follows a microservices architecture where each bounded context
is an independently deployable service. Services communicate over REST for
synchronous queries and RabbitMQ for asynchronous event-driven workflows.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Clients                                     │
│  ┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐    │
│  │ Angular Admin │  │ Customer Portal  │  │   Driver App     │    │
│  └──────┬───────┘  └────────┬─────────┘  └────────┬─────────┘    │
└─────────┼───────────────────┼─────────────────────┼───────────────┘
          │                   │                     │
          ▼                   ▼                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   YARP API Gateway                                 │
│  - Route requests to appropriate services                         │
│  - Rate limiting, authentication forwarding                        │
│  - CORS configuration, request logging                            │
└─────────┬──────────────────────────────────────────────────────────┘
          │
          ├──────────────────────────────────────────────────────────┐
          │              Internal Services                          │
          │                                                         │
          │  ┌─────────────┐  ┌──────────────┐  ┌──────────────┐  │
          │  │ Identity     │  │ Customer     │  │ Shipment     │  │
          │  │ Service      │  │ Service      │  │ Service      │  │
          │  └──────┬──────┘  └──────┬───────┘  └──────┬───────┘  │
          │         │                │                  │          │
          │  ┌──────┴──────┐  ┌─────┴────────┐  ┌─────┴────────┐  │
          │  │ Order       │  │ Warehouse    │  │ Fleet        │  │
          │  │ Service     │  │ Service      │  │ Service      │  │
          │  └──────┬──────┘  └──────┬───────┘  └──────┬───────┘  │
          │         │                │                  │          │
          │  ┌──────┴──────┐  ┌─────┴────────┐  ┌─────┴────────┐  │
          │  │ Driver      │  │ Route        │  │ Tracking     │  │
          │  │ Service     │  │ Planning     │  │ Service      │  │
          │  └──────┬──────┘  └──────┬───────┘  └──────┬───────┘  │
          │         │                │                  │          │
          │  ┌──────┴──────┐  ┌─────┴────────┐  ┌─────┴────────┐  │
          │  │ Billing     │  │ Notification │  │ Reporting    │  │
          │  │ Service     │  │ Service      │  │ Service      │  │
          │  └─────────────┘  └──────────────┘  └──────────────┘  │
          │                                                         │
          └──────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────────────┐
│              Infrastructure Services                               │
│  ┌──────────┐  ┌───────────┐  ┌──────────┐  ┌─────────────────┐  │
│  │ RabbitMQ │  │ Redis     │  │ SQL DBs  │  │ Central Logging │  │
│  │          │  │           │  │ (per svc)│  │ (Seq)           │  │
│  └──────────┘  └───────────┘  └──────────┘  └─────────────────┘  │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │ Monitoring: Prometheus + Grafana + OpenTelemetry            │  │
│  └──────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

## 2. Client Applications

### 2.1 Angular Admin
- URL: `https://admin.fedcarrier.com`
- Role: System administrators
- Features: User management, service configuration, rate management,
  shipment oversight, report generation, dashboard with KPIs
- Auth: JWT-based, role-protected routes

### 2.2 Customer Portal
- URL: `https://portal.fedcarrier.com`
- Role: End customers (shippers)
- Features: Account management, order placement, shipment tracking,
  invoice viewing, address management
- Auth: JWT-based, customer-scoped access

### 2.3 Driver App
- URL: `https://driver.fedcarrier.com` (PWA)
- Role: Delivery drivers
- Features: Assignment acceptance, status updates, route navigation,
  pickup/delivery confirmation, earnings view
- Auth: JWT-based, driver-scoped access
- Real-time: SignalR connection for live tracking and assignment push

## 3. API Gateway (YARP)

### 3.1 Responsibilities
- Route incoming HTTP requests to the correct microservice
- Terminate TLS at the gateway level
- Forward authentication tokens to downstream services
- Apply rate limiting per client/tenant
- Log all requests for observability
- Handle CORS for browser clients

### 3.2 Route Configuration

| Route Prefix            | Target Service         |
|-------------------------|------------------------|
| `/api/auth`             | Identity Service       |
| `/api/customers`        | Customer Service       |
| `/api/shipments`        | Shipment Service       |
| `/api/orders`           | Order Service          |
| `/api/warehouses`       | Warehouse Service      |
| `/api/fleet`            | Fleet Service          |
| `/api/drivers`          | Driver Service         |
| `/api/routes`           | Route Planning Service |
| `/api/tracking`         | Tracking Service       |
| `/api/billing`          | Billing Service        |
| `/api/notifications`    | Notification Service   |
| `/api/reports`          | Reporting Service      |

## 4. Core Services

### 4.1 Identity Service
- **Port**: 5001
- **DB**: SQL Server
- **Responsibilities**: User registration, login, JWT issuance,
  refresh token management, role management, MFA
- **Key Endpoints**:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `POST /api/auth/refresh`
  - `GET /api/auth/me`
  - `PUT /api/auth/roles` (admin)

### 4.2 Customer Service
- **Port**: 5002
- **DB**: SQL Server
- **Responsibilities**: Customer CRUD, profile management,
  address book, order placement
- **Key Endpoints**:
  - `GET/POST/PUT /api/customers`
  - `GET/PUT /api/customers/{id}/addresses`
  - `POST /api/customers/{id}/orders`

### 4.3 Shipment Service
- **Port**: 5003
- **DB**: SQL Server
- **Responsibilities**: Shipment lifecycle, status management,
  driver assignment, shipment items
- **Key Endpoints**:
  - `GET/POST/PUT /api/shipments`
  - `POST /api/shipments/{id}/assign-driver`
  - `PUT /api/shipments/{id}/status`

### 4.4 Order Service
- **Port**: 5004
- **DB**: SQL Server
- **Responsibilities**: Order lifecycle, order-to-shipment linkage,
  order status transitions
- **Key Endpoints**:
  - `GET/POST/PUT /api/orders`
  - `GET /api/orders/{id}/shipments`

### 4.5 Warehouse Service
- **Port**: 5005
- **DB**: SQL Server
- **Responsibilities**: Warehouse CRUD, inventory tracking,
  stock-in/stock-out, handoff to shipment
- **Key Endpoints**:
  - `GET/POST/PUT /api/warehouses`
  - `POST /api/warehouses/{id}/inventory`

### 4.6 Fleet Service
- **Port**: 5006
- **DB**: SQL Server
- **Responsibilities**: Vehicle CRUD, fleet assignment,
  maintenance scheduling, capacity tracking
- **Key Endpoints**:
  - `GET/POST/PUT /api/fleet`
  - `POST /api/fleet/{id}/assign-driver`

### 4.7 Driver Service
- **Port**: 5007
- **DB**: SQL Server
- **Responsibilities**: Driver CRUD, availability management,
  current location, assignment management
- **Key Endpoints**:
  - `GET/POST/PUT /api/drivers`
  - `PUT /api/drivers/{id}/location`
  - `PUT /api/drivers/{id}/availability`

### 4.8 Route Planning Service
- **Port**: 5008
- **DB**: SQL Server
- **Responsibilities**: Route optimization, waypoint sequencing,
  ETA calculation, traffic-aware routing
- **Key Endpoints**:
  - `POST /api/routes/optimize`
  - `GET /api/routes/{id}`

### 4.9 Tracking Service
- **Port**: 5009
- **DB**: SQL Server
- **Responsibilities**: Real-time GPS tracking, location storage,
  tracking history, ETA updates
- **Key Endpoints**:
  - `GET /api/tracking/{shipmentId}`
  - `POST /api/tracking/location` (driver push)

### 4.10 Billing Service
- **Port**: 5010
- **DB**: SQL Server
- **Responsibilities**: Invoice generation, payment tracking,
  rate card management, billing reconciliation
- **Key Endpoints**:
  - `GET/POST /api/billing/invoices`
  - `PUT /api/billing/invoices/{id}/pay`

### 4.11 Notification Service
- **Port**: 5011
- **DB**: SQL Server
- **Responsibilities**: Email/SMS/push dispatch, template management,
  notification preferences, delivery status
- **Key Endpoints**:
  - `POST /api/notifications`
  - `PUT /api/notifications/preferences`

### 4.12 Reporting Service
- **Port**: 5012
- **DB**: Read replicas of other services
- **Responsibilities**: Report generation, data aggregation,
  export (CSV/PDF), custom queries
- **Key Endpoints**:
  - `GET /api/reports/shipments`
  - `GET /api/reports/financial`
  - `GET /api/reports/drivers`
  - `GET /api/reports/export`

## 5. Communication Patterns

### 5.1 Synchronous (REST)
Used for request-response interactions where immediate feedback is needed.
All services expose RESTful HTTP APIs.

### 5.2 Asynchronous (RabbitMQ)
Used for event-driven workflows that do not require immediate response.

| Event                     | Publisher        | Subscriber(s)                        |
|---------------------------|------------------|--------------------------------------|
| ShipmentCreated           | Shipment Service | Tracking, Billing, Notification      |
| ShipmentAssigned          | Shipment Service | Driver, Notification                 |
| ShipmentDelivered         | Shipment Service | Billing, Notification, Reporting     |
| OrderPlaced               | Order Service    | Shipment, Notification               |
| DriverLocationUpdated     | Driver Service   | Tracking, Routing                    |
| PaymentReceived           | Billing Service  | Notification, Reporting              |

### 5.3 SignalR (Real-Time)
Used for live tracking updates pushed to the Customer Portal and Driver App.
The Tracking Service maintains SignalR hubs for connected clients.

### 5.4 Outbox Pattern
Each service writes domain events to an Outbox table in the same
transaction as the business data change. A background process
reliably publishes events to RabbitMQ, ensuring at-least-once delivery.

### 5.5 Saga Pattern
For distributed transactions spanning multiple services (e.g., order
placement triggers shipment creation, driver assignment, and billing).
Each step has a compensating action for rollback on failure.

## 6. Infrastructure

### 6.1 Message Broker - RabbitMQ
- Exchanges: `fedcarrier.direct`, `fedcarrier.topic`
- Queues per service for inbound events
- Dead-letter queues for failed message processing
- Message TTL and retry policies configured

### 6.2 Cache - Redis
- Shipment status caching for hot reads
- Driver location caching for real-time queries
- Rate limiting counters
- Distributed lock for idempotent operations

### 6.3 Database
- Each service owns its SQL database (SQL Server or PostgreSQL)
- Separate databases ensure data isolation and independent scaling
- Read replicas for Reporting Service
- Migrations managed via EF Core

### 6.4 Central Logging - Seq
- All services log structured JSON to Seq
- Correlation IDs included in every log entry
- Log levels: Debug (dev), Information (prod), Warning/Error (alerted)

### 6.5 Monitoring
- **Prometheus**: Scrapes metrics from all services (request duration,
  error rates, throughput)
- **Grafana**: Dashboards for service health, latency, error rates
- **OpenTelemetry**: Distributed tracing across service boundaries
- **Health Checks**: `/health` endpoint on every service, monitored by
  Kubernetes liveness/readiness probes

## 7. Security Architecture

### 7.1 Authentication Flow
1. User submits credentials via client app
2. Client app calls Identity Service `POST /api/auth/login`
3. Identity Service validates credentials, returns JWT access token
   (15 min expiry) and refresh token (7 days expiry)
4. Client includes `Authorization: Bearer <token>` in subsequent requests
5. Gateway validates token signature and forwards to downstream service
6. Each service validates the token and extracts user claims/roles

### 7.2 Authorization
- Role-based access control (RBAC) at the API level
- Policies defined per service (e.g., Admin only for user management)
- Claims propagated through the request pipeline

### 7.3 Transport Security
- All external traffic over HTTPS (TLS 1.3)
- Internal service-to-service communication over HTTP (within Docker network)
- RabbitMQ connections use TLS in production

### 7.4 Secrets Management
- Secrets stored in environment variables or Kubernetes Secrets
- No secrets committed to source control
- Database connection strings encrypted at rest

## 8. Deployment Architecture

### 8.1 Containerization
- Each service packaged as a Docker image
- Multi-stage Docker builds for optimized image size
- Images stored in a container registry

### 8.2 Kubernetes
- Namespace per environment (dev, staging, production)
- Deployments for each service with resource requests/limits
- Horizontal Pod Autoscaler based on CPU/memory metrics
- Service mesh (optional) for inter-service communication
- Ingress controller (YARP) for external traffic routing

### 8.3 CI/CD Pipeline
1. Code push triggers GitHub Actions workflow
2. Build Docker image, run unit/integration tests
3. Scan image for vulnerabilities
4. Push image to container registry
5. Deploy to staging environment
6. Run smoke tests against staging
7. Manual approval for production deployment
8. Rolling deployment to production