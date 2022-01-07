namespace Computer.Domain.Bus.Reactive.Contracts.Model;

public interface IBareEvent
{
    string EventId { get; }
    string CorrelationId { get; }
}

public interface IBusEvent : IBareEvent
{
    object? Param { get; }
    Type Type { get; }
}

public interface IBusEvent<out T> : IBareEvent
{
    T? Param { get; }
}