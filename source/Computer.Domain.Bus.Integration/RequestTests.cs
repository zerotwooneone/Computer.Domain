using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Computer.Domain.Bus.Contracts;
using Computer.Domain.Bus.Reactive;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace Computer.Domain.Bus.Integration;

public class RequestTests
{
    private IBus _bus;
    private IRequestService _requestService;
    private TestScheduler _scheduler;
    [SetUp]
    public void Setup()
    {
        _scheduler = new TestScheduler();
        _bus = new ReactiveBus(new TaskPoolScheduler(new TaskFactory()));//_scheduler);
        _requestService = new ReactiveResponseService(_bus);
    }

    [Test]
    public async Task Request1()
    {
        const string responseSubject = "responseSubject";
        const string requestEventId = "requestEventId";
        const string requestCorrelationId = "correlationId";
        const string responseEventId = "responseEventId";
        
        _scheduler.Start();
        
        Task<Response?> CreateResponse(Request? request, string eventId, string correlationId)
        {
            Assert.AreEqual(requestEventId, eventId);
            Assert.AreEqual(requestCorrelationId, correlationId);
            return Task.FromResult<Response?>(new Response(Guid.Parse("1b694445-3510-4a07-a920-0e40a3f18b91")));
        }
        const string requestSubject = "requestSubject";
        var sub = _requestService.Listen<Request, Response>(requestSubject, responseSubject, CreateResponse);
        
        var request = new Request("some test", 12.34d);
        
        var response = await _requestService.Request<Request, Response>(request, requestSubject, responseSubject,
            eventId: requestEventId,
            correlationId: requestCorrelationId).ConfigureAwait(false);
        
        Assert.AreNotEqual(responseEventId, response.EventId);
        Assert.AreEqual(requestCorrelationId, response.CorrelationId);
        
        Assert.IsNotNull(response);
    }
}

internal record Request(string SValue, double DValue);
internal record Response(Guid id);