using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Computer.Domain.Bus.Contracts;
using Computer.Domain.Bus.Reactive.Contracts;
using Computer.Domain.Bus.Reactive.Contracts.Model;
using Computer.Domain.Bus.Reactive.Model;

namespace Computer.Domain.Bus.Reactive;

public class CallbackBus : IBus
{
    private readonly IReactiveBus _bus;

    public CallbackBus(IReactiveBus bus)
    {
        _bus = bus;
    }

    public async Task Publish(string subject, Type type, object? obj, string? eventId = null, string? correlationId = null)
    {
        await _bus.Publish(subject, type, obj, eventId, correlationId).ConfigureAwait(false);
    }

    public async Task Publish(string subject, string? eventId = null, string? correlationId = null)
    {
        await _bus.Publish(subject, eventId, correlationId).ConfigureAwait(false);
    }

    public IDisposable Subscribe(string subject, Type type, IBus.ParameterCallback callback)
    {
        return _bus.Subscribe(subject, type)
            .SelectMany(busEvent => Observable.FromAsync(async _ =>
            {
                await callback(busEvent.Param, busEvent.Type, busEvent.EventId, busEvent.CorrelationId).ConfigureAwait(false);
            }))
            .Subscribe();
    }

    public IDisposable Subscribe(string subject, IBus.BareCallback callback)
    {
        return _bus.Subscribe(subject)
            .SelectMany(busEvent => Observable.FromAsync(async _ =>
            {
                await callback(busEvent.EventId, busEvent.CorrelationId).ConfigureAwait(false);
            }))
            .Subscribe();
    }
}