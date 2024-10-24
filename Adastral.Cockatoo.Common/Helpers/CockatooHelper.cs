using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Adastral.Cockatoo.Common.Helpers;

public static class CockatooHelper
{
    public static long GetMilliseconds()
    {
        double timestamp = Stopwatch.GetTimestamp();
        double ms = 1_000.0 * timestamp / Stopwatch.Frequency;
        return (long)ms;
    }
    public static string FormatTypeName(Type type)
    {
        var s = "null";
        return $"{type.Namespace}.{type.Name}, Assembly={type.Assembly?.FullName ?? s}";
    }
    public static long CalculateDirectorySize(DirectoryInfo dir)
    {
        long size = 0;
        var fis = dir.GetFiles();
        foreach (var item in fis)
        {
            size += item.Length;
        }
        var dirs = dir.GetDirectories();
        foreach (var item in dirs)
        {
            size += CalculateDirectorySize(item);
        }
        return size;
    }
    public static long CalculateDirectorySize(string directory)
    {
        if (!Directory.Exists(directory))
            return 0;
        return CalculateDirectorySize(new DirectoryInfo(directory));
    }
    public static long GetFileSize(string location)
    {
        if (!File.Exists(location))
            return 0;
        return new FileInfo(location).Length;
    }
    /// <summary>
    /// Format a Start and End timestamp into a string.
    /// </summary>
    /// <returns>HH hour(s) MM minute(s) SS second(s)</returns>
    public static string FormatDuration(DateTimeOffset start, DateTimeOffset end)
    {
        var span = end - start;
        return FormatDuration(span);
    }
    public static string FormatDuration(TimeSpan span)
    {
        var result = new List<string>();

        string pluralize(int c)
        {
            return c > 1 ? "s" : "";
        }

        if (span.TotalSeconds < 1)
        {
            return $"{span.TotalMilliseconds}ms";
        }
        
        if (span.Hours > 0)
            result.Add($"{span.Hours} hour" + pluralize(span.Hours));
        if (span.Minutes > 0)
            result.Add($"{span.Minutes} minute" + pluralize(span.Minutes));
        if (span.Seconds > 0)
            result.Add($"{span.Seconds}.{span.Milliseconds} second" + pluralize(span.Seconds));
        return string.Join(" ", result);
    }
    /// <summary>
    /// <inheritdoc cref="FormatDuration(System.DateTimeOffset,System.DateTimeOffset)"/>
    ///
    /// <para>Assumes that <paramref name="start"/> was created with <see cref="DateTimeOffset.UtcNow"/></para>
    /// </summary>
    public static string FormatDuration(DateTimeOffset start)
    {
        return FormatDuration(start, DateTimeOffset.UtcNow);
    }

    public static string GetSha256Hash(string content)
    {
        return GetSha256Hash(Encoding.UTF8.GetBytes(content));
    }
    public static string GetSha256Hash(byte[] content)
    {
        // Create a SHA256
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // ComputeHash - returns byte array
            byte[] bytes = sha256Hash.ComputeHash(content);

            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    public static string GetSha256Hash(Stream stream)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(stream);
            // Convert byte array to a string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}