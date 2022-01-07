using System.Reactive.Subjects;
using Computer.Domain.Bus.Reactive.Contracts.Model;

namespace Computer.Domain.Bus.Reactive.Model;

internal class BusSubject : IDisposable
{
    public string Name { get; }
    public IObservable<IBareEvent> BareObservable { get; }
    public IObservable<IBusEvent>? ParamObservable { get; }
    private readonly Subject<BusEvent> _subject;
    public Type? Type { get; }

    public BusSubject(
        string name,
        Subject<BusEvent> subject,
        IObservable<IBareEvent> bareObservable,
        IObservable<IBusEvent>? paramObservable,
        Type? type = null)
    {
        Name = name;
        BareObservable = bareObservable;
        ParamObservable = paramObservable;
        _subject = subject;
        Type = type;
    }

    public void Publish(BusEvent busEvent)
    {
        _subject.OnNext(busEvent);
    }

    public void Dispose()
    {
        _subject.Dispose();
    }
}