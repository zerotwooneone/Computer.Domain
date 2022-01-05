namespace Computer.Domain.Bus.Reactive.Model;

public record BusEvent
{
    public BusEvent(Type? type = null, object? param = null, string? eventId = null,
        string? correlationId = null)
    {
        Type = type;
        Param = param;
        EventId = eventId ?? Guid.NewGuid().ToString();
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }

    public Type? Type { get; init; }
    public object? Param { get; init; }
    public string EventId { get; init; }
    public string CorrelationId { get; init; }
}

public record BusEvent<T>
{
    public BusEvent(string subject, Type type, T param, string? eventId = null, string? correlationId = null)
    {
        Subject = subject;
        Type = type;
        Param = param;
        EventId = eventId ?? Guid.NewGuid().ToString();
        CorrelationId = correlationId ?? Guid.NewGuid().ToString();
    }

    public string Subject { get; init; }
    public Type Type { get; init; }
    public T Param { get; init; }
    public string EventId { get; init; }
    public string CorrelationId { get; init; }
}