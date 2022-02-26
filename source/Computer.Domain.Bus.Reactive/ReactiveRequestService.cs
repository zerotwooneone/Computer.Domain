using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Computer.Domain.Bus.Contracts;
using Computer.Domain.Bus.Contracts.Model;
using Computer.Domain.Bus.Reactive.Contracts;
using Computer.Domain.Bus.Reactive.Model;

namespace Computer.Domain.Bus.Reactive;

public class ReactiveRequestService : IRequestService
{
    private readonly IReactiveBus _bus;

    public ReactiveRequestService(IReactiveBus bus)
    {
        _bus = bus;
    }
    public async Task<IResponse> Request(
        object? request, 
        Type requestType, 
        string requestSubject, 
        Type responseType, 
        string responseSubject,
        string correlationId, 
        string? eventId = null, 
        CancellationToken cancellationToken = default)
    {
        var respSubject = GetResponseSubject(responseSubject, correlationId);
        
        async Task<GenericResponse> GetResponse()
        {
            var response = await _bus.Subscribe(respSubject, responseType)
                .FirstOrDefaultAsync()
                .ToTask(cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
            {
                return GenericResponse.CreateError("Request cancelled");
            }

            if (response == null)
            {
                return GenericResponse.CreateError("response was null");
            }
            return GenericResponse.CreateSuccess(response.Param, response.EventId, response.CorrelationId);    
        }

        var result = GetResponse();
        
        if (cancellationToken.IsCancellationRequested)
        {
            return GenericResponse.CreateError("Request cancelled");
        }
        await _bus.Publish(requestSubject, requestType, request, eventId, correlationId).ConfigureAwait(false);
        return await result.ConfigureAwait(false);
    }

    public IDisposable Listen(string requestSubject,
        Type requestType,
        string responseSubject,
        Type responseType, 
        IRequestService.CreateResponse createResponse,
        IRequestService.ErrorCallback? errorCallback = null)
    {
        return _bus.Subscribe(requestSubject, requestType)
            .SelectMany(b => Observable.FromAsync(async _ =>
            {
                (object?, Type) response;
                try
                {
                    response = await createResponse(b.Param, b.Type, b.EventId, b.CorrelationId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    try
                    {
                        errorCallback?.Invoke(e.ToString(), b.EventId, b.CorrelationId, b.Param, b.Type);
                    }
                    catch
                    {
                        //ignore
                    }
                    return Unit.Default;
                }

                if (!responseType.IsAssignableFrom(response.Item2))
                {
                    errorCallback?.Invoke(
                        $"response type was unexpected. wanted:{responseType} got:{response.Item2}",
                        b.EventId, b.CorrelationId, b.Param, b.Type);
                }
            
                var targetSubject = GetResponseSubject(responseSubject, b.CorrelationId);
                //respond to this correlation id
                try
                {
                    await _bus.Publish(targetSubject, responseType, response.Item1, correlationId: b.CorrelationId)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    try
                    {
                        errorCallback?.Invoke(e.ToString(), b.EventId, b.CorrelationId, b.Param, b.Type);
                    }
                    catch 
                    {
                        //ignore
                    }
                }
            
                //respond to the subject
                try
                {
                    await _bus.Publish(responseSubject, responseType, response.Item1, correlationId: b.CorrelationId)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    try
                    {
                        errorCallback?.Invoke(e.ToString(), b.EventId, b.CorrelationId, b.Param, b.Type);
                    }
                    catch
                    {
                        //ignore
                    }
                }

                return Unit.Default;
            }))
            .Subscribe();
    }
    
    private static string GetResponseSubject(string responseSubject, string correlationId)
    {
        return $"{responseSubject}:{correlationId}";
    }
}