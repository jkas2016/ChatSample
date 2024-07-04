using System.Net;
using System.Net.Mime;
using System.Security.Authentication;
using System.Text;
using ChatSample.Commons;
using ChatSample.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

GlobalDiagnosticsContext.Set("Environment", builder.Environment.EnvironmentName);
GlobalDiagnosticsContext.Set("ProjectRootPath", Directory.GetParent(Directory.GetCurrentDirectory())?.FullName);

// Builder > Logging
builder.Logging.ClearProviders();
// ILogger 를 사용하는 모든 곳에서 NLog 를 사용하도록 합니다.
// 기본적으로 프로젝트 루트경로에서 nlog.config 파일을 찾습니다.
// see also: https://betterstack.com/community/guides/logging/how-to-start-logging-with-nlog/#step-4-configuring-the-logger
builder.Host.UseNLog();
builder.Services.AddLogging(o =>
    o.Configure(x =>
        x.ActivityTrackingOptions =
            // TraceId == 단일 요청 (API 등과 같은) 내에서의 고유 ID
            ActivityTrackingOptions.TraceId |
            // SpanId == 단일 요청 내에서의 각각의 (메서드 등과 같은) 고유 ID
            ActivityTrackingOptions.SpanId));

// Builder > Configure
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<AccessLoggingMiddleware>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // If the request is for our hub...
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/chatHub"))
            {
                // Read the token out of the query string
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
    
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", corsPolicyBuilder => corsPolicyBuilder
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();
app.UseCors("AllowAllOrigins");
app.UseMiddleware<AccessLoggingMiddleware>();
app.UseExceptionHandler(applicationBuilder =>
{
    applicationBuilder.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        var statusCode = exception switch
        {
            BadHttpRequestException => HttpStatusCode.BadRequest,
            InvalidCredentialException => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError
        };

        // using static System.Net.Mime.MediaTypeNames;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(new
        {
            Code = (int)statusCode,
            Message = exception?.Message ?? "An error occurred.",
        });
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();