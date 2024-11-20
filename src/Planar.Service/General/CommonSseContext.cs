using Grpc.Core;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Planar.Service.General;

public class CommonSseContext
{
    private readonly HttpContext? _httpContext;
    private readonly IServerStreamWriter<GetRunningJobsLogReply>? _streamWriter;

    public CommonSseContext(HttpContext httpContext)
    {
        _httpContext = httpContext;
    }

    public CommonSseContext(IServerStreamWriter<GetRunningJobsLogReply> streamWriter)
    {
        _streamWriter = streamWriter;
    }

    public void Initialize()
    {
        const string key = "Content-Type";
        const string value = "text/event-stream";
        if (_httpContext == null) { return; }
        _httpContext.Response.Headers.Append(key, value);
    }

    public async Task WriteResponse(GetRunningJobsLogReply reply, CancellationToken cancellationToken)
    {
        var log = new LogEntity { Message = reply.Log };
        await WriteResponse(log, cancellationToken);
    }

    public async Task WriteResponse(LogEntity log, CancellationToken cancellationToken)
    {
        var text = log?.ToString();
        if (string.IsNullOrEmpty(text)) { return; }

        if (_httpContext != null)
        {
            await _httpContext.Response.WriteAsync($"{text}\n", cancellationToken: cancellationToken);
            await _httpContext.Response.Body.FlushAsync(cancellationToken);
        }

        if (_streamWriter != null)
        {
            var reply = new GetRunningJobsLogReply { Log = text };
            await _streamWriter.WriteAsync(reply, cancellationToken);
        }
    }
}