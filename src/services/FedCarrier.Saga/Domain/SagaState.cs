using System.ComponentModel.DataAnnotations;

namespace FedCarrier.Saga.Domain;

public enum SagaStep
{
    Initiated = 0,
    ShipmentCreated = 1,
    ShipmentDelivered = 2,
    InvoiceGenerated = 3,
    PaymentConfirmed = 4,
    Completed = 5
}

public enum SagaStatus
{
    InProgress = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}

public class SagaState
{
    [Key]
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ShipmentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid CustomerId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public SagaStep CurrentStep { get; set; } = SagaStep.Initiated;
    public SagaStatus Status { get; set; } = SagaStatus.InProgress;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
