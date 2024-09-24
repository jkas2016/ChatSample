using System.Diagnostics;
using System.Text;

namespace ChatSample.Commons;

public class AccessLoggingMiddleware(ILogger<AccessLoggingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // css, js 등 정적 파일은 로깅하지 않음
        if (context.Request.Path.Value?.StartsWith("/_blazor") == true ||
            context.Request.Path.Value?.StartsWith("/_framework") == true ||
            context.Request.Path.Value?.StartsWith("/_content") == true ||
            context.Request.Path.Value?.StartsWith("/css") == true ||
            context.Request.Path.Value?.StartsWith("/images") == true ||
            context.Request.Path.Value?.EndsWith(".map") == true ||
            context.Request.Path.Value?.EndsWith(".ico") == true ||
            context.Request.Path.Value?.EndsWith(".js") == true ||
            context.Request.Path.Value?.EndsWith(".css") == true)
        {
            await next(context);
            return;
        }
        
        // 요청 시간 측정 시작
        var stopwatch = Stopwatch.StartNew();
        
        // Request 로깅
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body, Encoding.UTF8).ReadToEndAsync();
        context.Request.Body.Seek(0, SeekOrigin.Begin);
        
        await next(context);
        
        // 응답 시간 측정 종료
        stopwatch.Stop();

        logger.LogInformation(
            "{Method} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds}ms -> {requestBody}",
            context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, requestBody
        );
    }
}