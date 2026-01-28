namespace StrategicoAudit.Api.Middleware;

public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // RequestId
        var requestId = context.Request.Headers["X-Request-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(requestId))
            requestId = Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;

        // Basic client info
        context.Items["IpAddress"] = context.Connection.RemoteIpAddress?.ToString();
        context.Items["UserAgent"] = context.Request.Headers.UserAgent.ToString();

        // Echo request id back to client for tracing
        context.Response.Headers["X-Request-Id"] = requestId;

        await _next(context);
    }
}
