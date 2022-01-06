using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Computer.Domain.Bus.Contracts;
using Computer.Domain.Bus.Reactive.Model;

namespace Computer.Domain.Bus.Reactive;

public class ReactiveBus : IBus
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

    public IDisposable Subscribe(string subject, Type type, IBus.ParameterCallback callback)
    {
        IDisposable? subscription = null;

        IDisposable InnerSubscribe(BusSubject busSubject1)
        {
            return busSubject1.Observable
                .SelectMany(async p =>
                {
                    await callback(p.Param,type, p.EventId, p.CorrelationId).ConfigureAwait(false);
                    return Unit.Default;
                })
                .Retry() //todo: need to do something with errors
                .Subscribe();
        }

        _subjectsByName.AddOrUpdate(subject,
            s =>
            {
                var newSubject = new Subject<BusEvent>();
                var observable = ObservableFactory(newSubject, subject);
                var busSubject = new BusSubject(s, newSubject, observable, type);
                subscription = InnerSubscribe(busSubject);
                
                return busSubject;
            }, (s, bs) =>
            {
                subscription = InnerSubscribe(bs);
                return bs;
            });
        return subscription ?? _disposableDummy;
    }

    public IDisposable Subscribe(string subject, IBus.BareCallback callback)
    {
        IDisposable? subscription = null;
        IDisposable InnerSubscribe(BusSubject busSubject1)
        {
            return busSubject1.Observable
                .SelectMany(async p =>
                {
                    await callback(p.EventId, p.CorrelationId).ConfigureAwait(false);
                    return Unit.Default;
                })
                .Subscribe();
        }
        _subjectsByName.AddOrUpdate(subject,
            s =>
            {
                var newSubject = new Subject<BusEvent>();
                var observable = ObservableFactory(newSubject, subject);
                var busSubject = new BusSubject(s, newSubject, observable, null);
                subscription = InnerSubscribe(busSubject);
                
                return busSubject;
            }, (s, bs) =>
            {
                subscription = InnerSubscribe(bs);
                return bs;
            });
        return subscription ?? _disposableDummy;
    }
    
    private IObservable<BusEvent> ObservableFactory(Subject<BusEvent> subject, string subjectName)
    {
        return subject
            .AsObservable()
            .ObserveOn(_scheduler)
            .Publish()
            .RefCount()
            .Finally(() =>
            {
                _subjectsByName.Remove(subjectName, out _);
            });
    }
    
    private readonly DisposableDummy _disposableDummy = new();
    private class DisposableDummy : IDisposable
    {
        public void Dispose()
        {
        }
    }
}