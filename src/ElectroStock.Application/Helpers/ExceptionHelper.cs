using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Helpers
{
    /// <summary>
    /// Utility for categorizing exceptions in the Application Layer.
    /// Business exceptions (validation, not-found) bubble up to controllers.
    /// Technical exceptions (db, timeout) are wrapped with friendly messages.
    /// 
    /// This MVP approach is simple and pragmatic. 
    /// Can be refactored to custom exception hierarchy if app scales.
    /// </summary>
    public class ExceptionHelper
    {
        /// <summary>
        /// Determines if an exception is a business/validation exception that should bubble up to the controller.
        /// These exceptions will be handled by the controller to return appropriate HTTP status codes (400, 404, etc.).
        /// </summary>
        public static bool IsBusinessException(Exception ex)
        {
            return ex is ArgumentException ||
                   ex is ArgumentNullException ||
                   ex is InvalidOperationException ||
                   ex is KeyNotFoundException ||
                   ex is UnauthorizedAccessException ||
                   ex is InvalidDataException ||
                   ex is ValidationException;
        }

        /// <summary>
        /// Determines if an exception is a technical/infrastructure exception that should be wrapped.
        /// These exceptions will be logged and wrapped in a generic exception to hide implementation details.
        /// </summary>
        public static bool IsTechnicalException(Exception ex)
        {
            return ex is System.Data.Common.DbException ||
                   ex is TimeoutException ||
                   ex is IOException ||
                   ex is System.Net.Http.HttpRequestException ||
                   ex is System.Net.Sockets.SocketException;
        }
    }
}
