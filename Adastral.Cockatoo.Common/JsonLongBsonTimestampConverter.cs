using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace Adastral.Cockatoo.Common;

public class JsonLongBsonTimestampConverter : JsonConverter<BsonTimestamp>
{
    public override BsonTimestamp Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return new BsonTimestamp(reader.GetInt64());
    }

    public override void Write(
        Utf8JsonWriter writer,
        BsonTimestamp value,
        JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}