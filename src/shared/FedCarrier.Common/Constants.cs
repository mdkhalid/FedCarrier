namespace FedCarrier.Common;

public static class Constants
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string IdempotencyKeyHeader = "X-Idempotency-Key";
    public const string ServiceNameHeader = "X-Service-Name";
}

public static class EnvironmentNames
{
    public const string Development = "Development";
    public const string Staging = "Staging";
    public const string Production = "Production";
}
