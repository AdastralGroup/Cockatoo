using System.ComponentModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class StorageFileModel : BaseGuidModel
{
    public static string CollectionName => "storage_file";

    public StorageFileModel()
        : base()
    {
        ContentType = "application/octet-stream";
        Location = "";
        CreatedAtTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        UpdatedAtTimestamp = CreatedAtTimestamp;
        Sha256Hash = "".PadRight(64, '0');
        Size = null;
    }
    
    /// <summary>
    /// Sha256 Hash of the content at the Location specified.
    /// </summary>
    public string Sha256Hash { get; set; }
    public bool HasHash()
    {
        return !string.IsNullOrEmpty(Sha256Hash) || Sha256Hash != "".PadRight(64, '0');
    }

    /// <summary>
    /// Location to this file in the current configured S3-compatible bucket.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// MIME type for the file.
    /// </summary>
    public string ContentType { get; set; }

    #region Size
    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Size of the file in bytes</para>
    /// </summary>
    [DefaultValue(null)]
    [BsonIgnoreIfNull]
    public string? Size { get; set; }
    /// <summary>
    /// Get/Parse the value for <see cref="Size"/>
    /// <summary>
    public long? GetSize()
    {
        if (string.IsNullOrEmpty(Size))
            return null;
        return long.Parse(Size);
    }
    /// <summary>
    /// Set the value for <see cref="Size"/>
    /// </summary>
    public void SetSize(long value)
    {
        Size = value.ToString();
    }
    #endregion
    
    #region Created At
    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Since Unix Epoch (Seconds, UTC)</para>
    /// </summary>
    public string CreatedAtTimestamp { get; set; }
    /// <summary>
    /// Get the value of <see cref="CreatedAtTimestamp"/>
    /// </summary>
    public long GetCreatedAtTimestamp() => long.Parse(UpdatedAtTimestamp);
    /// <summary>
    /// Set the value of <see cref="CreatedAtTimestamp"/>
    /// </summary>
    public void SetCreatedAtTimestamp(long value)
    {
        CreatedAtTimestamp = value.ToString();
    }
    /// <summary>
    /// Set the value of <see cref="CreatedAtTimestamp"/> to the current timestamp.
    /// </summary>
    public void SetCreatedAtTimestamp()
    {
        SetUpdatedAtTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    #endregion
    
    #region Updated At
    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Since Unix Epoch (Seconds, UTC)</para>
    /// </summary>
    public string UpdatedAtTimestamp { get; set; }
    /// <summary>
    /// Get the value of <see cref="UpdatedAtTimestamp"/>
    /// </summary>
    /// <returns></returns>
    public long GetUpdatedAtTimestamp() => long.Parse(UpdatedAtTimestamp);
    /// <summary>
    /// Set the value of <see cref="UpdatedAtTimestamp"/>.
    /// </summary>
    public void SetUpdatedAtTimestamp(long value)
    {
        UpdatedAtTimestamp = value.ToString();
    }
    /// <summary>
    /// Set the value of <see cref="UpdatedAtTimestamp"/> to the current timestamp.
    /// </summary>
    public void SetUpdatedAtTimestamp()
    {
        SetUpdatedAtTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    #endregion
}