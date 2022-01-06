using Computer.Domain.Bus.Contracts;
using Computer.Domain.Bus.Contracts.Model;
using Computer.Domain.Bus.Reactive.Model;

namespace Computer.Domain.Bus.Reactive;

public class ReactiveResponseService : IRequestService
{
    private readonly IBus _bus;

    public ReactiveResponseService(IBus bus)
    {
        _bus = bus;
    }
    public async Task<IResponse> Request(object? request, Type requestType, string requestSubject, Type responseType, string responseSubject,
        string correlationId, string? eventId = null, CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ts = new TaskCompletionSource<IResponse>();

        var respSubject = GetResponseSubject(responseSubject, correlationId);
        Task ResponseCallback(object? param, Type? type, string responseEventId, string responseCorrelationId)
        {
            ts.TrySetResult(GenericResponse.CreateSuccess(param, responseEventId, responseCorrelationId));
            
            //make sure to cancel the token AFTER we try to set the Task result
            //stop the subscription
            cts.Cancel();
            
            return Task.CompletedTask;
        }
        if (cts.IsCancellationRequested)
        {
            ts.TrySetCanceled(cts.Token);
            return GenericResponse.CreateError("Request cancelled");
        }
        var subscription = InnerSubscribe(_bus, respSubject, responseType, ResponseCallback);
        cts.Token.Register(() =>
        {
            ts.TrySetCanceled(cts.Token);
            subscription.Dispose();
        });
        if (cts.Token.IsCancellationRequested)
        {
            ts.TrySetCanceled(cts.Token);
            return GenericResponse.CreateError("Request cancelled");
        }
        await _bus.Publish(requestSubject, requestType, request, eventId, correlationId).ConfigureAwait(false);
        return await ts.Task.ConfigureAwait(false);
    }

    public IDisposable Listen(string requestSubject,
        Type requestType,
        string responseSubject,
        Type responseType, 
        IRequestService.CreateResponse createResponse)
    {
        async Task OnRequest(object? param, Type? type, string eventId, string correlationId)
        {
            var response = await createResponse(param, type, eventId, correlationId).ConfigureAwait(false);

            if (requestType.IsAssignableFrom(response.Item2))
            {
                throw new InvalidOperationException(
                    $"response type was unexpected. wanted:{responseType} got:{response.Item2}");
            }
            
            var targetSubject = GetResponseSubject(responseSubject, correlationId);
            //respond to this correlation id
            await _bus.Publish(targetSubject, responseType, response.Item1, correlationId: correlationId)
                .ConfigureAwait(false);
            
            //respond to the subject
            await _bus.Publish(responseSubject, responseType, response.Item1, correlationId: correlationId).ConfigureAwait(false);
        }
        return _bus.Subscribe(requestSubject, requestType, OnRequest);
    }
    
    private static string GetResponseSubject(string responseSubject, string correlationId)
    {
        return $"{responseSubject}:{correlationId}";
    }

    private static IDisposable InnerSubscribe(
        IBus bus, 
        string responseSubject,
        Type responseType,
        IBus.ParameterCallback callback)
    {
        return bus.Subscribe(responseSubject, responseType, callback);
    }
}