using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Adastral.Cockatoo.Services.WebApi;

public class FileUploadFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParameters = context.ApiDescription.ParameterDescriptions
            .Where(paramDesc => paramDesc.IsFromForm());
        if(formParameters.Any())
        {
            // already taken care by swashbuckle. no need to add explicitly.
            return;
        }
        if(operation.RequestBody != null)
        {
            // NOT required for form type
            return;
        }
        if (context.ApiDescription.HttpMethod == HttpMethod.Post.Method)
        {
            var uploadFileMediaType = new OpenApiMediaType()
            {
                Schema = new OpenApiSchema()
                {
                    Type = "object",
                    Properties =
                    {
                        ["files"] = new OpenApiSchema()
                        {
                            Type = "array",
                            Items = new OpenApiSchema()
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "Form File Upload"
                            },
                            Description = "Form File Upload"
                        }
                    },
                    Required = new HashSet<string>() { "files" },
                    Description = "Form File Upload"
                }
            };
            var uploadStreamFileType = new OpenApiMediaType()
            {
                Schema = new OpenApiSchema()
                {
                    Type = "string",
                    Format = "binary",
                    Description = "Binary File Upload"
                }
            };

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>(){ {"multipart/form-data", uploadFileMediaType},
                {"application/octet-stream", uploadStreamFileType} },
                Required = true
            };
        }
    }
}
internal static class MvcApiExplorerExtensions
{
    internal static bool IsFromForm(this ApiParameterDescription apiParameter)
    {
        var source = apiParameter.Source;
        var elementType = apiParameter.ModelMetadata?.ElementType;

        return (source == BindingSource.Form || source == BindingSource.FormFile)
            || (elementType != null && typeof(IFormFile).IsAssignableFrom(elementType));
    }
}