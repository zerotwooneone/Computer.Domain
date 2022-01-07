using System.Reactive.Linq;
using Computer.Domain.Bus.Reactive.Contracts.Model;

namespace Computer.Domain.Bus.Reactive.Contracts;

public interface IReactiveBus
{
    Task Publish(string subject, Type type, object? obj, string? eventId = null, string? correlationId = null);
    Task Publish(string subject, string? eventId = null, string? correlationId = null);
    IObservable<IBareEvent> Subscribe(string subject);
    IObservable<IBusEvent> Subscribe(string key, Type type);
}

public static class BusExtensions
{
    public static Task Publish<T>(
        this IReactiveBus reactiveBus,
        string subject,
        T? obj,
        string? eventId = null,
        string? correlationId = null)
    {
        return reactiveBus.Publish(subject, typeof(T), obj, eventId, correlationId);
    }

    public static IObservable<IBusEvent<T>> Subscribe<T>(this IReactiveBus reactiveBus, string subject)
    {
        var type = typeof(T);

        IBusEvent<T> Callback(IBusEvent busEvent)
        {
            if (!type.IsAssignableFrom(busEvent.Type))
            {
                throw new InvalidCastException($"Cannot cast. expecting:{type} actual:{busEvent.Type}");
            }

            var param = (T?)busEvent.Param;
            return new BusEvent<T>(param, busEvent.EventId, busEvent.CorrelationId);
        }

        return reactiveBus.Subscribe(subject, type)
            .Select(Callback);
    }

    private record BusEvent<T>(T? Param, string EventId, string CorrelationId) : IBusEvent<T>;
}

