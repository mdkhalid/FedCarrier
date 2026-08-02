# FedCarrier Message Schema Registry

All inter-service communication flows through RabbitMQ. Events are published to the
topic exchange `fedcarrier.events` (durable) with a routing key derived from the event
type name (lowercased, `event` suffix stripped). Failed messages are retried up to
`MaxRetries` (default 3) via a `.retry` queue with `RetryDelaySeconds` (default 5) TTL,
then routed to the dead-letter exchange `fedcarrier.dlx`.

## Conventions

- Every event derives from `IntegrationEvent` (in `FedCarrier.Contracts`).
- `EventId`: unique event identifier (assigned at creation).
- `EventType`: set by the event bus to the concrete CLR type name before publishing.
- `OccurredOn`: UTC timestamp of when the business fact happened.
- `CorrelationId`: propagated from the originating HTTP request (`X-Correlation-Id`
  header) through every downstream event for tracing.
- Content-Type: `application/json` (UTF-8). Messages are persistent.
- Routing key: `orderplaced`, `shipmentcreated`, `invoicegenerated`, etc.
  (type name lowercased, `event` suffix removed).

## Event Catalog

### Domain Events (fact happened)

| Event                   | Producer      | Consumers            | Key Fields                                                        |
|-------------------------|---------------|----------------------|-------------------------------------------------------------------|
| `OrderPlacedEvent`      | Order         | Saga, Notification   | OrderId, CustomerId, CustomerName, TotalAmount                    |
| `OrderStatusChangedEvent` | Order       | Saga (cancel path)   | OrderId, Status                                                   |
| `ShipmentCreatedEvent`  | Shipment      | Saga                 | ShipmentId, OrderId, Origin, Destination                          |
| `ShipmentAssignedEvent` | Shipment      | (future: tracking)   | ShipmentId, DriverId, VehicleId                                   |
| `ShipmentStatusChangedEvent` | Shipment | (future: tracking) | ShipmentId, OrderId, Status                                    |
| `ShipmentDeliveredEvent`| Shipment      | Saga                 | ShipmentId, OrderId                                               |
| `InvoiceGeneratedEvent` | Billing       | Saga                 | InvoiceId, ShipmentId, CustomerId, InvoiceNumber, TotalAmount     |
| `PaymentConfirmedEvent` | Billing       | Saga                 | InvoiceId, ShipmentId, CustomerId, TotalAmount                    |

### Command Events (saga orchestrator → service)

| Event                      | Producer | Consumer      | Key Fields                                          |
|----------------------------|----------|---------------|-----------------------------------------------------|
| `CreateShipmentCommandEvent` | Saga   | Shipment      | OrderId, CustomerId, Origin, Destination            |
| `CancelShipmentCommandEvent` | Saga   | Shipment      | OrderId                                             |
| `CreateInvoiceCommandEvent` | Saga    | Billing       | OrderId, ShipmentId, CustomerId, Amount             |
| `NotifyCustomerCommandEvent`| Saga    | Notification  | CustomerId, CustomerName, Title, Message, Channel   |

## Retry & Dead-Letter Behavior

- On handler exception: message is published to the `.retry` queue with header
  `x-retry-count` incremented, then acknowledged.
- The `.retry` queue has `x-message-ttl = RetryDelaySeconds * 1000` and dead-letters
  back to `fedcarrier.events` with the original routing key, so the message is
  re-delivered to the main queue after the delay.
- After `MaxRetries` attempts the message is published to `fedcarrier.dlx` with routing
  key `<queue>.dlq`, carrying `x-failure-reason` and `x-correlation-id` headers.
- Consumers should be idempotent (re-delivery can occur at-least-once).

## Versioning

Messages are additive-only: fields are added, never renamed or removed. On a breaking
change, introduce a new event type (e.g. `OrderPlacedV2Event`) rather than mutating an
existing schema.
