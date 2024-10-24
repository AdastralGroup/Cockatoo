using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class TaskMutexModel
    : BaseGuidModel
{
    public TaskMutexModel()
        : base()
    {
        TaskClassType = "";
        TaskName = "";
        Released = false;
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public const string CollectionName = "task_mutex";
    /// <summary>
    /// Type name for the Class that the Task is running on.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public string TaskClassType { get; set; }
    /// <summary>
    /// Name of the Method or Task that this Mutex is for.
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public string TaskName { get; set; }
    /// <summary>
    /// Options that were used for this Task
    /// </summary>
    [BsonIgnoreIfNull]
    public Dictionary<string, object>? Options { get; set; }
    /// <summary>
    /// Has this mutex been released?
    /// </summary>
    public bool Released { get; set; }
    /// <summary>
    /// Timestamp when this Task Mutex was created at. (UTC, Seconds)
    /// </summary>
    public BsonTimestamp CreatedAt { get; set; }
    /// <summary>
    /// Timestamp when this Task Mutex was last updated at. (UTC, Seconds)
    /// </summary>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BsonTimestamp? UpdatedAt { get; set; }
}