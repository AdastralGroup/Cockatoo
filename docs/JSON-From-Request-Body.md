When creating a new route on an ApiController and you wish to use the Request Body as a JSON, use the following attributes instead of ASP.NET Core using Json.NET (we want to use System.Text.Json)
```csharp
using Adastral.Cockatoo.Services.WebApi;

// ..
    [HttpPost]
    public async Task<ActionResult> CreateToken(
        string userId,
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] UserControllerApiV1CreateTokenRequest)
// ..
```

This is highly recommended for API Routes that do not deal with form-like bodies, and to make sure that `System.Text.Json` is being used for JSON serialization/deserialization for a request body instead of using `Newtonsoft.Json`. This is done since `System.Text.Json` is usually faster than `Newtonsoft.Json` and its built-in to C#.