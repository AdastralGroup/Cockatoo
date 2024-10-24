namespace Adastral.Cockatoo.Common;

public interface IBinarySerialize
{
    /// <summary>
    /// Write the contents of the current struct into the <paramref name="writer"/> provided.
    /// </summary>
    public void Serialize(BinaryWriter writer);
    /// <summary>
    /// Deserialize at the current position of the <paramref name="reader"/> provided into the current struct.
    /// </summary>
    public void Deserialize(BinaryReader reader);
}