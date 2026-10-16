using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SmartPantry.ClientesExternos;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public HttpMethod? UltimoMetodo { get; private set; }
    public Uri? UltimaUri { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => _responder = responder;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UltimoMetodo = request.Method;
        UltimaUri = request.RequestUri;
        return Task.FromResult(_responder(request));
    }
}