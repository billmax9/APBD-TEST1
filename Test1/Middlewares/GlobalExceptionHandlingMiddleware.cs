using System.Net;
using System.Text.Json;
using Test1.Exceptions;

namespace Test1.Middlewares;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception occured");
            await HandleExceptionAsync(context, e);
        }
    }


    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        int statusCode;
        string status;

        switch (ex)
        {
            case NotFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                status = "NotFound";
                break;
            case ConflictException:
                statusCode = (int)HttpStatusCode.Conflict;
                status = "Conflict";
                break;
            case DataValidationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                status = "BadRequest";
                break;
            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                status = "InternalServerError";
                break;
        }

        context.Response.StatusCode = statusCode;

        var response = new
        {
            Status = status,
            StatusCode = statusCode,
            Message = ex.Message
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}