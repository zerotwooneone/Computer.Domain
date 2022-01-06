namespace Computer.Domain.Bus.Reactive.Model;

internal sealed record BusEvent
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