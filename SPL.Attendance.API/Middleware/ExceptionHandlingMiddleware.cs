using System.Net;
using System.Text.Json;
using SPL.Attendance.API.DTOs;

namespace SPL.Attendance.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next,
                                           ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                KeyNotFoundException e => (HttpStatusCode.NotFound, e.Message),
                InvalidOperationException e => (HttpStatusCode.BadRequest, e.Message),
                UnauthorizedAccessException e => (HttpStatusCode.Unauthorized, e.Message),
                ArgumentException e => (HttpStatusCode.BadRequest, e.Message),
                _ => (HttpStatusCode.InternalServerError,
                                                  "An unexpected error occurred.")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = (int)statusCode;

            var payload = JsonSerializer.Serialize(
                ApiResponse<object>.Fail(message),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

            return context.Response.WriteAsync(payload);
        }
    }
}
