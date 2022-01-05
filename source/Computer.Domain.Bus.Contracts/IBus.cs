namespace Computer.Domain.Bus.Contracts;

public interface IBus
{
    Task Publish(string subject, Type type, object? obj, string? eventId = null, string? correlationId = null);
    Task Publish(string subject, string? eventId = null, string? correlationId = null);
    IDisposable Subscribe(string subject, BareCallback callback);
    IDisposable Subscribe(string key, Type type, ParameterCallback callback);
    
    public delegate Task ParameterCallback(object? param, Type? type, string eventId, string correlationId);
    public delegate Task BareCallback(string eventId, string correlationId);
}

public static class BusExtensions
{
    public delegate Task SubscribeCallback<in T>(T? param, string eventId, string correlationId);
    public static Task Publish<T>(
        this IBus bus,
        string subject,
        T? obj,
        string? eventId = null,
        string? correlationId = null)
    {
        return bus.Publish(subject, typeof(T), obj, eventId, correlationId);
    }
    
    public static Task Respond<T>(
        this IBus bus,
        string subject,
        T? obj,
        string correlationId,
        string? eventId = null)
    {
        var respSubject = GetResponseSubject(subject, correlationId);
        return bus.Publish<T>(respSubject, obj, eventId, correlationId);
    }

    public static IDisposable Subscribe<T>(this IBus bus, string subject, SubscribeCallback<T> callback)
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

        return bus.Subscribe(subject, type, Callback);
    }

    public static async Task Request<TRequest, TResponse>(this IBus bus,
        TRequest? request,
        string requestSubject,
        string responseSubject, 
        SubscribeCallback<TResponse> callback, 
        string correlationId, string? eventId = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var respSubject = GetResponseSubject(responseSubject, correlationId);
        var subscription = InnerSubscribe(bus, request, requestSubject, respSubject, callback, eventId, correlationId, cancellationToken);
        
        cancellationToken.Register(() => { subscription.Dispose(); });
        if (!cancellationToken.IsCancellationRequested)
        {
            await bus.Publish<TRequest>(requestSubject, request, eventId, correlationId);
        }
    }

    private static string GetResponseSubject(string responseSubject, string correlationId)
    {
        return $"{responseSubject}:{correlationId}";
    }

    private static IDisposable InnerSubscribe<TRequest, TResponse>(IBus bus, TRequest? request, string requestSubject,
        string responseSubject, SubscribeCallback<TResponse> callback, string? eventId, string? correlationId,
        CancellationToken cancellationToken)
    {
        async Task ResponseCallback(TResponse? param, string responseEventId, string responseCorrelationId)
        {
            await callback(param, responseEventId, responseCorrelationId);
        }

        return bus.Subscribe<TResponse>(responseSubject, ResponseCallback);
        
    }
}