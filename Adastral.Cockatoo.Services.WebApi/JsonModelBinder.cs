using System.Text.Json;
using Adastral.Cockatoo.Common;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Adastral.Cockatoo.Services.WebApi;

public class JsonModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        using (var reader = new StreamReader(bindingContext.HttpContext.Request.Body))
        {
            var body = await reader.ReadToEndAsync().ConfigureAwait(continueOnCapturedContext: false);

            var value = JsonSerializer.Deserialize(body, bindingContext.ModelType, BaseService.SerializerOptions);

            bindingContext.Result = ModelBindingResult.Success(value);
        }
    }
}