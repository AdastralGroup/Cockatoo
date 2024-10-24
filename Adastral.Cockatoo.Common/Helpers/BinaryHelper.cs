using System.Net.Http.Headers;

namespace Adastral.Cockatoo.Common.Helpers;

/// <summary>
/// Helper class for serializing/deserializing structs/things that use <see cref="BinaryWriter"/> and <see cref="BinaryReader"/>
/// </summary>
public static class BinaryHelper
{
    /// <summary>
    /// Write the content in <paramref name="self"/> into the <paramref name="outputStream"/> provided.
    /// </summary>
    public static void ToStream<TStruct>(this TStruct self, Stream outputStream)
        where TStruct : struct, IBinarySerialize
    {
        using var writer = new BinaryWriter(outputStream);
        self.Serialize(writer);
    }

    /// <summary>
    /// Serialize <paramref name="self"/> to an instance of <see cref="StreamContent"/>
    /// </summary>
    public static StreamContent ToContent<TStruct>(this TStruct self)
        where TStruct : struct, IBinarySerialize
    {
        var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms))
        {
            self.Serialize(writer);
        }
        ms.Seek(0, SeekOrigin.Begin);

        var sc = new StreamContent(ms);
        sc.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return sc;
    }
}