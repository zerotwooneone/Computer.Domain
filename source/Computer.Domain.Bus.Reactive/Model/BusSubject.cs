using System.Reactive.Subjects;

namespace Computer.Domain.Bus.Reactive.Model;

internal class BusSubject : IEquatable<BusSubject>, IDisposable
{
    public string Name { get; }
    private readonly Subject<BusEvent> _subject;
    public IObservable<BusEvent> Observable { get; }
    public Type? Type { get; }

    public BusSubject(
        string name,
        Subject<BusEvent> subject,
        IObservable<BusEvent> observable,
        Type? type = null)
    {
        Name = name;
        Observable = observable;
        _subject = subject;
        Type = type;
    }

    public void Publish(BusEvent busEvent)
    {
        _subject.OnNext(busEvent);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is BusSubject other && Equals(other);
    }

    public bool Equals(BusSubject? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public static bool operator ==(BusSubject? left, BusSubject? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BusSubject? left, BusSubject? right)
    {
        return !Equals(left, right);
    }

    public void Dispose()
    {
        _subject.Dispose();
    }
}