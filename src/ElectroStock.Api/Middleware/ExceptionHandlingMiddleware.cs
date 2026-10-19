using ElectroStock.Application.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ElectroStock.API.Middleware
{
    public class ExceptionHandlingMiddleware
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
                // try run the called service
                await _next(context);
            }
            catch (Exception ex)
            {
                // if error prunes we catch it and handles it
                await HandleExceptionAsync(context, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {

            // defaults of statuscode, message (title)
            var statusCode = StatusCodes.Status500InternalServerError;
            var title = "An unexpected error occurred.";

            _logger.LogError(exception, "Unhandled exception occurred");

            if (exception is UnauthorizedAccessException)
            {
                statusCode = StatusCodes.Status401Unauthorized;
                title = exception.Message;
            }
            else if (exception is KeyNotFoundException)
            {
                statusCode = StatusCodes.Status404NotFound;
                title = exception.Message;
            }
            else if (ExceptionHelper.IsBusinessException(exception))
            {
                statusCode = StatusCodes.Status400BadRequest;
                title = exception.Message;
            }
            
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var problemDetails = new
            {
                title,                // "Cannot modify confirmed invoice"
                status = statusCode,          // 400
                instance = context.Request.Path // "/api/admin/staff-creation"
            };

            var jsonResponse = JsonSerializer.Serialize(problemDetails);


            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
