using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Common;

public abstract class BaseService
{
    protected IServiceProvider _services;
    protected BaseService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Called when all services have been added to the collection.
    /// </summary>
    /// <returns></returns>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public static JsonSerializerOptions SerializerOptions =>
        new()
        {
            
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            Converters =
            {
                new JsonLongBsonTimestampConverter()
            }
        };
}