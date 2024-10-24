// Copied from AutoUpdater.NET
// https://github.com/ravibpatel/AutoUpdater.NET/blob/d3ea38f2d9ca5282e294d71755569d3aa6521d49/AutoUpdater.NET/UpdateInfoEventArgs.cs
//
// <<< Begin License
// MIT License
// 
// Copyright (c) 2012-2024 RBSoft
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// >>> End License
//
// Changes;
// - Removed properties; IsUpdateAvailable, Error, InstalledVersion.
// - Added static methods Serialize and Deserialize.

using System.ComponentModel.DataAnnotations;
using System.Xml;
using System.Xml.Serialization;

namespace Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;

[XmlRoot("item")]
public class UpdateInfoEventArgs
{
    public static UpdateInfoEventArgs? Deserialize(string content)
    {
        var xmlSerializer = new XmlSerializer(typeof(UpdateInfoEventArgs));
        var xmlTextReader = new XmlTextReader(new StringReader(content)) {XmlResolver = null};
        return (UpdateInfoEventArgs?)xmlSerializer.Deserialize(xmlTextReader);
    }
    public string Serialize()
    {
        var xml = new XmlSerializer(typeof(UpdateInfoEventArgs));
        using var sww = new StringWriter();
        using (var wr = XmlWriter.Create(sww))
        {
            xml.Serialize(wr, this);
        }
        var content = sww.ToString();
        return content;
    }

    /// <summary>
    ///     Download URL of the update file.
    /// </summary>
    [Required]
    [XmlElement("url")]
    public string DownloadUrl { get; set; } = "";

    /// <summary>
    ///     URL of the webpage specifying changes in the new update.
    /// </summary>
    [XmlElement("changelog")]
    public string? ChangelogUrl { get; set; }

    /// <summary>
    ///     Returns newest version of the application available to download.
    /// </summary>
    [Required]
    [XmlElement("version")]
    public string CurrentVersion { get; set; } = "1.0.0.0";

    /// <summary>
    ///     Shows if the update is required or optional.
    /// </summary>
    [XmlElement("mandatory")]
    public MandatoryData Mandatory { get; set; } = new();

    /// <summary>
    ///     Executable path of the updated application relative to installation directory.
    /// </summary>
    [XmlElement("executable")]
    public string? ExecutablePath { get; set; }

    /// <summary>
    ///     Command line arguments used by Installer.
    /// </summary>
    [XmlElement("args")]
    public string? InstallerArgs { get; set; }

    /// <summary>
    ///     Checksum of the update file.
    /// </summary>
    [XmlElement("checksum")]
    public CheckSumData? CheckSum { get; set; }

    public class MandatoryData
    {
        /// <summary>
        ///     Value of the Mandatory field.
        /// </summary>
        [XmlText]
        public bool Value { get; set; }

        /// <summary>
        ///     If this is set and 'Value' property is set to true then it will trigger the mandatory update only when current
        ///     installed version is less than value of this property.
        /// </summary>
        [XmlAttribute("minVersion")]
        public string? MinimumVersion { get; set; }

        /// <summary>
        ///     Mode that should be used for this update.
        /// </summary>
        [XmlAttribute("mode")]
        public AUDNMandatoryKind UpdateMode { get; set; }
    }
    /// <summary>
    /// Checksum class to fetch the XML values for checksum.
    /// </summary>
    public class CheckSumData
    {
        /// <summary>
        /// Hash of the file.
        /// </summary>
        [Required]
        [XmlText]
        public string Value { get; set; } = "";

        /// <summary>
        /// Hash algorithm that generated the hash.
        /// </summary>
        [Required]
        [XmlAttribute("algorithm")]
        public string HashingAlgorithm { get; set; } = "";
    }
}