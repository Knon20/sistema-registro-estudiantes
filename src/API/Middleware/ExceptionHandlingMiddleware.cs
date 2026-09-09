using Domain.Exceptions;

namespace API.Middleware;

public sealed record ErrorResponse(string Code, string Message);

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (InvalidEnrollmentException ex)
        {
            await WriteAsync(context, StatusCodes.Status422UnprocessableEntity, ex.Code, ex.Message);
        }
        catch (DuplicateEntityException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, ex.Code, ex.Message);
        }
        catch (EntityNotFoundException ex)
        {
            await WriteAsync(context, StatusCodes.Status404NotFound, ex.Code, ex.Message);
        }
        catch (ConcurrencyException ex)
        {
            await WriteAsync(context, StatusCodes.Status409Conflict, ex.Code, ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, ex.Code, ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new ErrorResponse(code, message));
    }
}
