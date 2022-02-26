namespace Computer.Domain.Bus.Contracts;

public interface IBus
{
    Task Publish(string subject, Type type, object? obj, string? eventId = null, string? correlationId = null);
    Task Publish(string subject, string? eventId = null, string? correlationId = null);
    IDisposable Subscribe(
        string subject, 
        BareCallback callback,
        ErrorCallback? errorCallback = null);
    IDisposable Subscribe(
        string key, 
        Type type, 
        ParameterCallback callback,
        ErrorCallback? errorCallback = null);
    
    public delegate Task ParameterCallback(object? param, Type? type, string eventId, string correlationId);
    public delegate Task BareCallback(string eventId, string correlationId);
    public delegate void ErrorCallback(string reason, string? eventId, string? correlationId, object? param, Type? type);
}

public static class BusExtensions
{
    public delegate Task SubscribeCallback<in T>(T? param, string eventId, string correlationId);
    public delegate void ErrorCallback<in T>(string reason, string? eventId, string? correlationId, T? param);
    public static Task Publish<T>(
        this IBus bus,
        string subject,
        T? obj,
        string? eventId = null,
        string? correlationId = null)
    {
        return bus.Publish(subject, typeof(T), obj, eventId, correlationId);
    }

    public static IDisposable Subscribe<T>(this IBus bus, 
        string subject, 
        SubscribeCallback<T> callback,
        ErrorCallback<T>? errorCallback = null)
    {
        var type = typeof(T);

        Task Callback(object? p, Type? t, string eventId, string correlationId)
        {
            if (!type.IsAssignableFrom(t))
            {
                return Task.CompletedTask;
            }

            var param = (T?)p;
            return callback(param, eventId, correlationId);
        }

        void OnError(string reason, string? eid, string? cid, object? o, Type? t)
        {
            errorCallback(reason, eid, cid, default);
        }

        var onError = errorCallback == null
            ? (IBus.ErrorCallback?)null
            : OnError;
        return bus.Subscribe(subject, type, Callback, onError);
    }
}