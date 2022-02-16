using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Computer.Domain.Bus.Contracts;
using Computer.Domain.Bus.Reactive;
using NUnit.Framework;

namespace Computer.Domain.Bus.Integration;

public class RequestTests
{
    private IBus _bus;
    private IRequestService _requestService;
    private ReactiveBus _reactiveBus;

    [SetUp]
    public void Setup()
    {
        _reactiveBus = new ReactiveBus(new TaskPoolScheduler(new TaskFactory()));
        _bus = new CallbackBus(_reactiveBus);
        _requestService = new ReactiveRequestService(_reactiveBus);
    }

    [Test]
    public async Task Request1()
    {
        const string responseSubject = "responseSubject";
        const string requestEventId = "requestEventId";
        const string requestCorrelationId = "correlationId";
        const string responseEventId = "responseEventId";
        const string requestString = "some test";
        var responseGuid = Guid.Parse("1b694445-3510-4a07-a920-0e40a3f18b91");
        
        async Task<Response?> CreateResponse(Request? request, string eventId, string correlationId)
        {
            await Task.Delay(1).ConfigureAwait(false);
            Assert.AreEqual(requestEventId, eventId);
            Assert.AreEqual(requestCorrelationId, correlationId);
            Assert.AreEqual(requestString, request?.SValue);
            return new Response(responseGuid);
        }
        const string requestSubject = "requestSubject";
        using var sub = _requestService.Listen<Request, Response>(requestSubject, responseSubject, CreateResponse);

        var request = new Request(requestString, 12.34d);
        
        var response = await _requestService.Request<Request, Response>(request, requestSubject, responseSubject,
            eventId: requestEventId,
            correlationId: requestCorrelationId).ConfigureAwait(false);
        
        Assert.AreNotEqual(responseEventId, response.EventId);
        Assert.AreEqual(requestCorrelationId, response.CorrelationId);
        Assert.AreEqual(responseGuid, response.Obj?.id);
        
        Assert.IsNotNull(response);
    }
}

internal record Request(string SValue, double DValue);
internal record Response(Guid id);