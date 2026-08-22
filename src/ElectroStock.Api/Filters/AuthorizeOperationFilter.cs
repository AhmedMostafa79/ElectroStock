using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ElectroStock.API.Filters
{
    /// <summary>
    /// Automatically adds 401 (Unauthorized) and 403 (Forbidden) responses 
    /// to ALL endpoints that have an [Authorize] attribute.
    /// </summary>
    public class AuthorizeOperationFilter: IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                .Union(context.MethodInfo.GetCustomAttributes(true))
                .OfType<AuthorizeAttribute>()
                .Any() ?? false;


            if (hasAuthorize)
            {
                // Add 401 Unauthorized response
                operation.Responses.TryAdd("401", new OpenApiResponse
                {
                    Description = "Unauthorized. The request lacks valid authentication credentials."
                });

                // Add 403 Forbidden response
                operation.Responses.TryAdd("403", new OpenApiResponse
                {
                    Description = "Forbidden. The authenticated user does not have permission to access this resource."
                });
            }
        }
    }
}
