namespace Computer.Domain.Bus.Contracts.Model;

public interface IResponse
{
    object? Obj { get; }
    string? EventId { get; }
    string? CorrelationId { get; }
    bool Success { get; }
    string? ErrorReason { get; }
}

public interface IResponse<out T>
{
    T? Obj { get; }
    string? EventId { get; }
    string? CorrelationId { get; }
    bool Success { get; }
    string? ErrorReason { get; }
}