using System;
using System.Threading.Tasks;
using Computer.Domain.Bus.Contracts;
using Computer.Domain.Bus.Reactive;
using Microsoft.Reactive.Testing;
using NUnit.Framework;

namespace Computer.Domain.Bus.Integration;

public class RequestTests
{
    private IBus _bus;
    private TestScheduler _scheduler;
    [SetUp]
    public void Setup()
    {
        _scheduler = new TestScheduler();
        _bus = new ReactiveBus(_scheduler);
    }

    [Test]
    public void Request1()
    {
        const string responseSubject = "responseSubject";
        const string requestEventId = "requestEventId";
        const string requestCorrelationId = "correlationId";
        Task OnRequestReceived(Request? param, string eventId, string correlationId)
        {
            Assert.AreEqual(requestEventId, eventId);
            Assert.AreEqual(requestCorrelationId, correlationId);
            
            _bus.Respond<Response>(
                responseSubject, 
                new Response(Guid.Parse("1b694445-3510-4a07-a920-0e40a3f18b91")), 
                correlationId: correlationId);
            return Task.CompletedTask;
        }
        const string requestSubject = "requestSubject";
        var sub = _bus.Subscribe<Request>(requestSubject, OnRequestReceived);
        
        Response? response = null;
        Task Callback(Response? param, string eventId, string correlationId)
        {
            response = param;
            Assert.AreNotEqual(requestEventId, eventId);
            Assert.AreEqual(requestCorrelationId, correlationId);
            return Task.CompletedTask;
        }

        var request = new Request("some test", 12.34d);
        _bus.Request<Request, Response>(request, requestSubject, responseSubject,
            Callback, 
            eventId: requestEventId,
            correlationId: requestCorrelationId);
        
        Assert.IsNull(response);
        _scheduler.Start();
        Assert.IsNotNull(response);
    }
}

internal record Request(string SValue, double DValue);
internal record Response(Guid id);