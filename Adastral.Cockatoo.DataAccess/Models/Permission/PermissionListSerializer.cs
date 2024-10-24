using System.Drawing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Adastral.Cockatoo.DataAccess.Models;

public class PermissionListSerializer : EnumAsStringListSerializer<PermissionKind>
{
}

public class EnumAsStringListSerializer<TEnum> : SerializerBase<List<TEnum>>
    where TEnum : struct, Enum
{
    public override void Serialize(BsonSerializationContext ctx, BsonSerializationArgs args, List<TEnum> value)
    {
        ctx.Writer.WriteStartArray();
        for (int i = 0; i < value.Count; i++)
        {
            ctx.Writer.WriteString(value[i].ToString());
        }
        ctx.Writer.WriteEndArray();
    }

    public override List<TEnum> Deserialize(BsonDeserializationContext ctx, BsonDeserializationArgs args)
    {
        if (ctx.Reader.CurrentBsonType == BsonType.Array)
        {
            ctx.Reader.ReadStartArray();
            var res = new List<TEnum>();
            var lazy = new Lazy<BsonStringSerializer>();
            var currentType = ctx.Reader.ReadBsonType();
            do
            {
                if (currentType == BsonType.String)
                {
                    var item = lazy.Value.Deserialize(ctx);
                    if (Enum.TryParse<TEnum>(item?.ToString(), out var x))
                    {
                        res.Add(x);
                    }
                }
                if (currentType != BsonType.EndOfDocument)
                {
                    currentType = ctx.Reader.ReadBsonType();
                }
            }
            while (currentType != BsonType.EndOfDocument);
            ctx.Reader.ReadEndArray();
            return res;
        }
        else
        {
            return [];
        }
    }
}