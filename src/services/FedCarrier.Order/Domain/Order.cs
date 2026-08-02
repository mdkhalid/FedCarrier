namespace FedCarrier.Order.Domain;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public class Order : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class BaseEntity
{
    public Guid Id { get; set; }
}
