using FluentValidation;
using Vara.Api.Models;
using Vara.Api.Services.Analysis;
using Vara.Api.Services.Plugins;

namespace Vara.Api.Middleware;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — not an error worth logging
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {ExceptionType}", ex.GetType().Name);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse { TraceId = context.TraceIdentifier };

        switch (exception)
        {
            case FeatureAccessDeniedException denied:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                response.Code = "FEATURE_ACCESS_DENIED";
                response.Message = denied.Message;
                break;

            case QuotaExceededException quota:
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.Code = "QUOTA_EXCEEDED";
                response.Message = quota.Message;
                break;

            case ValidationException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Code = "VALIDATION_ERROR";
                response.Message = "Validation failed";
                response.Errors = validationEx.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ErrorMessage).ToArray());
                break;

            case ArgumentException argEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Code = "INVALID_ARGUMENT";
                response.Message = argEx.Message;
                break;

            case OperationCanceledException:
                context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                response.Code = "REQUEST_TIMEOUT";
                response.Message = "Request timed out";
                break;

            case PluginNotFoundException ex:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Code = "PLUGIN_NOT_FOUND";
                response.Message = ex.Message;
                break;

            case PluginDisabledException ex:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                response.Code = "PLUGIN_DISABLED";
                response.Message = ex.Message;
                break;

            case HttpRequestException:
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                response.Code = "EXTERNAL_API_ERROR";
                response.Message = "An external API request failed. Please try again.";
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Code = "INTERNAL_ERROR";
                response.Message = "An unexpected error occurred";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}
