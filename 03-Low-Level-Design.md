# Low-Level Design - FedCarrier

## 1. Architecture Patterns

### 1.1 Clean Architecture (per service)
```
┌─────────────────────────────────────────┐
│              API Layer                   │
│  Controllers, Filters, Middleware       │
├─────────────────────────────────────────┤
│           Application Layer              │
│  CQRS Handlers, DTOs, Validators,       │
│  Interfaces, Pipeline Behaviors        │
├─────────────────────────────────────────┤
│            Domain Layer                  │
│  Entities, Value Objects, Domain Events,│
│  Repository Interfaces, Domain Services │
├─────────────────────────────────────────┤
│         Infrastructure Layer             │
│  EF Core DbContext, Repositories,       │
│  RabbitMQ Publisher, Cache, Logging     │
└─────────────────────────────────────────┘
```

### 1.2 CQRS
- Commands: Write operations (Create, Update, Delete, Assign)
- Queries: Read operations (Get, Search, List)
- Separate handlers for commands and queries
- MediatR pipeline for cross-cutting concerns

### 1.3 Repository Pattern
- `IRepository<T>` interface in Domain layer
- `EfRepository<T>` implementation in Infrastructure layer
- Unit of Work pattern for transactional consistency

### 1.4 FluentValidation
- Validator classes per command/query/DTO
- Declarative validation rules
- Integrated into MediatR pipeline

### 1.5 Outbox Pattern
- `OutboxMessage` entity persisted in same transaction as domain change
- Background `OutboxProcessor` reads pending messages and publishes to RabbitMQ
- Idempotency key on each event to prevent duplicates

## 2. Shipment Service (Detailed Example)

### 2.1 Database Schema

#### Shipments Table
| Column          | Type          | Constraints              |
|-----------------|---------------|--------------------------|
| Id              | uniqueidentifier | PK, default NEWID()    |
| OrderId         | uniqueidentifier | FK -> Orders.Id       |
| Origin          | nvarchar(200) | NOT NULL                 |
| Destination     | nvarchar(200) | NOT NULL                 |
| Status          | int           | NOT NULL, default 0      |
| AssignedDriverId| uniqueidentifier | FK -> Drivers.Id (nullable) |
| VehicleId       | uniqueidentifier | FK -> Fleet.Id (nullable) |
| CreatedAt       | datetime2     | NOT NULL, default GETUTCDATE() |
| UpdatedAt       | datetime2     | NOT NULL, default GETUTCDATE() |
| CreatedBy       | nvarchar(100) | NOT NULL                 |

#### ShipmentItems Table
| Column          | Type          | Constraints              |
|-----------------|---------------|--------------------------|
| Id              | uniqueidentifier | PK, default NEWID()    |
| ShipmentId      | uniqueidentifier | FK -> Shipments.Id     |
| Description     | nvarchar(500) | NOT NULL                 |
| Weight          | decimal(18,3) | NOT NULL                 |
| Quantity        | int           | NOT NULL, default 1      |
| Price           | decimal(18,2) | NOT NULL                 |

#### StatusHistory Table
| Column          | Type          | Constraints              |
|-----------------|---------------|--------------------------|
| Id              | uniqueidentifier | PK, default NEWID()    |
| ShipmentId      | uniqueidentifier | FK -> Shipments.Id     |
| Status          | int           | NOT NULL                 |
| Notes           | nvarchar(1000)| NULL                     |
| ChangedAt       | datetime2     | NOT NULL, default GETUTCDATE() |
| ChangedBy       | nvarchar(100) | NOT NULL                 |

### 2.2 Domain Entities

```csharp
public class Shipment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public ShipmentStatus Status { get; set; }
    public Guid? AssignedDriverId { get; set; }
    public Guid? VehicleId { get; set; }
    public List<ShipmentItem> Items { get; set; } = new();
    public List<StatusHistory> StatusHistory { get; set; } = new();
}

public class ShipmentItem : BaseEntity
{
    public Guid ShipmentId { get; set; }
    public string Description { get; set; }
    public decimal Weight { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class StatusHistory : BaseEntity
{
    public Guid ShipmentId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string Notes { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; }
}

public enum ShipmentStatus
{
    Pending = 0,
    Assigned = 1,
    InTransit = 2,
    Delivered = 3,
    Cancelled = 4
}
```

### 2.3 Commands

#### CreateShipmentCommand
```csharp
public class CreateShipmentCommand : IRequest<Guid>
{
    public Guid OrderId { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public List<ShipmentItemDto> Items { get; set; }
}

public class ShipmentItemDto
{
    public string Description { get; set; }
    public decimal Weight { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

#### AssignDriverCommand
```csharp
public class AssignDriverCommand : IRequest<Unit>
{
    public Guid ShipmentId { get; set; }
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
}
```

#### UpdateShipmentStatusCommand
```csharp
public class UpdateShipmentStatusCommand : IRequest<Unit>
{
    public Guid ShipmentId { get; set; }
    public ShipmentStatus Status { get; set; }
    public string Notes { get; set; }
    public string ChangedBy { get; set; }
}
```

### 2.4 Queries

#### GetShipmentQuery
```csharp
public class GetShipmentQuery : IRequest<ShipmentDto>
{
    public Guid Id { get; set; }
}

public class ShipmentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public ShipmentStatus Status { get; set; }
    public Guid? AssignedDriverId { get; set; }
    public Guid? VehicleId { get; set; }
    public List<ShipmentItemDto> Items { get; set; }
    public List<StatusHistoryDto> StatusHistory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StatusHistoryDto
{
    public ShipmentStatus Status { get; set; }
    public string Notes { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; }
}
```

#### SearchShipmentsQuery
```csharp
public class SearchShipmentsQuery : IRequest<PagedResult<ShipmentSummaryDto>>
{
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public ShipmentStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ShipmentSummaryDto
{
    public Guid Id { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public ShipmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

### 2.5 Domain Events

```csharp
public class ShipmentCreatedEvent : IDomainEvent
{
    public Guid ShipmentId { get; set; }
    public Guid OrderId { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ShipmentAssignedEvent : IDomainEvent
{
    public Guid ShipmentId { get; set; }
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class ShipmentStatusChangedEvent : IDomainEvent
{
    public Guid ShipmentId { get; set; }
    public ShipmentStatus PreviousStatus { get; set; }
    public ShipmentStatus NewStatus { get; set; }
    public string Notes { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class ShipmentDeliveredEvent : IDomainEvent
{
    public Guid ShipmentId { get; set; }
    public DateTime DeliveredAt { get; set; }
    public string DeliveredBy { get; set; }
}
```

### 2.6 API Controllers

```csharp
[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly ISender _mediator;

    public ShipmentsController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create(CreateShipmentCommand cmd)
        => Ok(await _mediator.Send(cmd));

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentDto>> Get(Guid id)
        => Ok(await _mediator.Send(new GetShipmentQuery { Id = id }));

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ShipmentSummaryDto>>> Search(
        [FromQuery] SearchShipmentsQuery query)
        => Ok(await _mediator.Send(query));

    [HttpPut("{id}/assign-driver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> AssignDriver(Guid id, AssignDriverCommand cmd)
    {
        cmd.ShipmentId = id;
        await _mediator.Send(cmd);
        return Ok();
    }

    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> UpdateStatus(Guid id, UpdateStatusCommand cmd)
    {
        cmd.ShipmentId = id;
        await _mediator.Send(cmd);
        return Ok();
    }
}
```

### 2.7 Validators

```csharp
public class CreateShipmentCommandValidator : AbstractValidator<CreateShipmentCommand>
{
    public CreateShipmentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Origin).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Destination).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty().MinimumSize(1);
        RuleForEach(x => x.Items).SetValidator(new ShipmentItemDtoValidator());
    }
}

public class ShipmentItemDtoValidator : AbstractValidator<ShipmentItemDto>
{
    public ShipmentItemDtoValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Weight).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
    }
}
```

### 2.8 Outbox Integration

```csharp
public class ShipmentDomainEventHandler : INotificationHandler<ShipmentCreatedEvent>
{
    private readonly IOutboxRepository _outbox;
    public ShipmentDomainEventHandler(IOutboxRepository outbox) => _outbox = outbox;

    public async Task Handle(ShipmentCreatedEvent notification, CancellationToken ct)
    {
        await _outbox.SaveAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = notification.ShipmentId.ToString(),
            EventType = nameof(ShipmentCreatedEvent),
            Content = JsonSerializer.Serialize(notification),
            OccurredOn = DateTime.UtcNow,
            Processed = false
        }, ct);
    }
}
```

## 3. Event Contracts (Shared)

### 3.1 Shipment Events

| Event Name            | Publisher      | Payload                          |
|-----------------------|----------------|----------------------------------|
| ShipmentCreated       | Shipment       | ShipmentCreatedEvent             |
| ShipmentAssigned      | Shipment       | ShipmentAssignedEvent            |
| ShipmentStatusChanged | Shipment       | ShipmentStatusChangedEvent       |
| ShipmentDelivered     | Shipment       | ShipmentDeliveredEvent           |

### 3.2 Order Events

| Event Name       | Publisher  | Payload              |
|------------------|------------|----------------------|
| OrderPlaced      | Order      | OrderPlacedEvent     |
| OrderCancelled   | Order      | OrderCancelledEvent  |

### 3.3 Driver Events

| Event Name             | Publisher  | Payload                  |
|------------------------|------------|--------------------------|
| DriverLocationUpdated  | Driver     | DriverLocationUpdatedEvent |
| DriverAssigned         | Driver     | DriverAssignedEvent      |

### 3.4 Billing Events

| Event Name          | Publisher  | Payload                |
|---------------------|------------|------------------------|
| InvoiceGenerated    | Billing    | InvoiceGeneratedEvent  |
| PaymentReceived     | Billing    | PaymentReceivedEvent   |

## 4. Shared Contracts

### 4.1 Common Response Envelope
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public string CorrelationId { get; set; }
}
```

### 4.2 Error Response
```csharp
public class ErrorResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]> Details { get; set; }
}
```

## 5. Cross-Cutting Concerns

### 5.1 Global Exception Handling
- `ExceptionHandlingMiddleware` catches all unhandled exceptions
- Returns structured `ErrorResponse` with correlation ID
- Maps domain exceptions to appropriate HTTP status codes

### 5.2 Correlation ID
- `CorrelationIdMiddleware` generates or propagates X-Correlation-Id header
- Correlation ID included in all logs and outgoing messages
- Returned in response headers for client reference

### 5.3 Logging
- Structured logging with Serilog
- Log context includes correlation ID, service name, user identity
- Log levels: Debug, Information, Warning, Error, Fatal

### 5.4 Caching
- Redis cache for read-heavy queries (e.g., shipment status lookups)
- Cache key pattern: `{service}:{entity}:{id}`
- Cache invalidation on write operations via RabbitMQ pub/sub

### 5.5 Idempotency
- Idempotency key header `X-Idempotency-Key` on POST/PUT endpoints
- Idempotency store in Redis with TTL
- Duplicate requests return the same response as the first request