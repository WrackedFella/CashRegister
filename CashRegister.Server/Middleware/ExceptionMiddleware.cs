using System.Net;
using System.Text.Json;
using Serilog;

namespace CashRegister.Server.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception with structured logging
            _logger.LogError(exception, "An unhandled exception occurred while processing request {RequestPath} with method {RequestMethod}",
                context.Request.Path, context.Request.Method);

            // Determine the appropriate status code
            var statusCode = exception switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                _ => (int)HttpStatusCode.InternalServerError
            };

            // Set the response
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var errorResponse = new
            {
                error = new
                {
                    message = exception.Message,
                    type = exception.GetType().Name,
                    timestamp = DateTime.UtcNow,
                    requestId = context.TraceIdentifier
                }
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}