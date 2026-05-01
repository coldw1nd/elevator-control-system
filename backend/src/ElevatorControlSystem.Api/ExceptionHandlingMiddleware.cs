using Microsoft.AspNetCore.Mvc;
using ElevatorControlSystem.Application;

namespace ElevatorControlSystem.Api;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(httpContext, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext httpContext,
        Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            ValidationApplicationException => (
                StatusCodes.Status400BadRequest,
                "Ошибка валидации",
                exception.Message),

            NotFoundApplicationException => (
                StatusCodes.Status404NotFound,
                "Объект не найден",
                exception.Message),

            ConflictApplicationException => (
                StatusCodes.Status409Conflict,
                "Конфликт операции",
                exception.Message),

            UnauthorizedApplicationException => (
                StatusCodes.Status401Unauthorized,
                "Требуется авторизация",
                exception.Message),

            ForbiddenApplicationException => (
                StatusCodes.Status403Forbidden,
                "Доступ запрещен",
                exception.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "Произошла непредвиденная ошибка. Приложение сохранило устойчивость работы.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Необработанное исключение.");
        }
        else
        {
            _logger.LogWarning(exception, "Обработано прикладное исключение.");
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails);
    }
}
