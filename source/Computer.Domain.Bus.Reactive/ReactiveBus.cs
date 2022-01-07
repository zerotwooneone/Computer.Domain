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

public class ReactiveBus : IReactiveBus
{
    private readonly IScheduler _scheduler;
    private readonly ConcurrentDictionary<string, BusSubject> _subjectsByName;

    public ReactiveBus(IScheduler scheduler)
    {
        _scheduler = scheduler;
        _subjectsByName = new();
    }

    public Task Publish(string subject, Type type, object? obj, string? eventId = null, string? correlationId = null)
    {
        if (!_subjectsByName.TryGetValue(subject, out var busSubject))
        {
            return Task.CompletedTask;    
        }
        var busEvent = new BusEvent(type, obj, eventId, correlationId);
        busSubject.Publish(busEvent);
        return Task.CompletedTask;
    }

    public Task Publish(string subject, string? eventId = null, string? correlationId = null)
    {
        if (!_subjectsByName.TryGetValue(subject, out var busSubject))
        {
            return Task.CompletedTask;    
        }
        var busEvent = new BusEvent(eventId: eventId, correlationId: correlationId);
        busSubject.Publish(busEvent);
        return Task.CompletedTask;
    }

    public IObservable<IBusEvent> Subscribe(string subject, Type type)
    {
        var bs = _subjectsByName.GetOrAdd(subject,
            s =>
            {
                var newSubject = new Subject<BusEvent>();
                var (bareObservable, paramObservable) = ObservableFactory(newSubject, subject, type);
                var busSubject = new BusSubject(s, newSubject, bareObservable, paramObservable, type);
                
                return busSubject;
            });
        if (bs.ParamObservable == null)
        {
            throw new InvalidOperationException($"this subject {subject} is not configured to return a param");
        }

        if (!type.IsAssignableFrom(bs.Type))
        {
            throw new InvalidOperationException($"this subject {subject} cannot return type {type}. Use {bs.Type?.ToString() ?? "no type param"} instead");
        }
        return bs.ParamObservable;
    }

    public IObservable<IBareEvent> Subscribe(string subject)
    {
        var bs = _subjectsByName.GetOrAdd(subject,
            s =>
            {
                var newSubject = new Subject<BusEvent>();
                var (bareObservable, _) = ObservableFactory(newSubject, subject, null);
                var busSubject = new BusSubject(s, newSubject, bareObservable, null, null);
                
                return busSubject;
            });
        return bs.BareObservable;
    }
    
    private (IObservable<BusEvent>, IObservable<IBusEvent>?) ObservableFactory(
        Subject<BusEvent> subject, 
        string subjectName,
        Type? type)
    {
        var eventObservable = subject
            .AsObservable()
            .ObserveOn(_scheduler)
            .Publish()
            .RefCount()
            .Finally(() =>
            {
                _subjectsByName.Remove(subjectName, out _);
            });

        ParamEvent ToParamEvent(BusEvent b)
        {
            if (!type.IsAssignableFrom(b.Type))
            {
                throw new InvalidCastException($"unable to cast. expected:{type} actual:{b.Type}");
            }
            return new ParamEvent(b.Param, b.Type, b.EventId, b.CorrelationId);
        }

        var paramObservable = type == null
            ? null
            : eventObservable
                .Select(ToParamEvent);
        return (eventObservable, paramObservable);
    }

    private record ParamEvent(object? Param, Type Type, string EventId, string CorrelationId) : IBusEvent;
    
    private readonly DisposableDummy _disposableDummy = new();
    private class DisposableDummy : IDisposable
    {
        public void Dispose()
        {
        }
    }
}