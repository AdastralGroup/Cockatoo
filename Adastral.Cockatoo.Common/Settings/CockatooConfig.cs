using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Adastral.Cockatoo.Common.Helpers;
using Infisical.Sdk;
using NLog;

namespace Adastral.Cockatoo.Common;

[XmlRoot("Config")]
public class CockatooConfig
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly bool _readOnly = false;
    public CockatooConfig()
        : this(false)
    { }
    public CockatooConfig(bool ro = false)
    {
        _readOnly = ro;
        PublicUrl = "http://localhost:5280";

        MongoDB = new();
        BasicAuth = new();
        Authentik = new();
        Ldap = new();
        Storage = new();
        AspNET = new();
        Southbank = new();
        Redis = new();
    }

    public void ResetToDefault()
    {
        if (_readOnly)
            return;
        SetValuesToDefault(this);
    }

    public void ReadFromInfisical(InfisicalClient client, string environment, string projectId)
    {
        if (_readOnly)
            return;
        ResetToDefault();
        ImportFromInfisical(this, client, environment, projectId);
    }

    public void ReadFromXmlFile(string location)
    {
        if (_readOnly)
            return;
        if (string.IsNullOrEmpty(location))
        {
            throw new ArgumentException($"Must not be null or empty", location);
        }
        if (!File.Exists(location))
        {
            throw new FileNotFoundException($"Could not find file {location}");
        }
        ResetToDefault();
        ImportFromXmlFile(location);
    }

    public void ReadFromEnvironmentFile(string location)
    {
        if (_readOnly)
            return;
        if (string.IsNullOrEmpty(location))
        {
            throw new ArgumentException($"Must not be null or empty", location);
        }
        if (!File.Exists(location))
        {
            throw new FileNotFoundException($"Could not find file {location}");
        }
        ResetToDefault();
        var handler = new EnvironmentFileHandler(location, false);
        ImportFromEnvironment(handler, this);
    }

    public void WriteIntoInfisical(InfisicalClient client, string environment, string projectId)
    {
        if (_readOnly)
            return;
        if (string.IsNullOrEmpty(environment))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(environment));
        }
        if (string.IsNullOrEmpty(projectId))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(projectId));
        }

        ExportToInfisical(this, environment, projectId, client);
    }

    public void WriteToEnvironmentFile(string location)
    {
        if (_readOnly)
            return;
        var lines = GenerateEnvironmentFileLines(this);
        File.WriteAllLines(location, lines);
    }

    public void WriteToXmlFile(string location)
    {
        if (_readOnly)
            return;
        var xml = new XmlSerializer(GetType());
        using var sww = new StringWriter();
        using (var wr = XmlWriter.Create(sww, new() {Indent = true}))
        {
            xml.Serialize(wr, this);
        }
        var content = sww.ToString();
        File.WriteAllText(location, content);
    }

    public void Read()
    {
        SetValuesToDefault(this);
        if (FeatureFlags.InfisicalEnable)
        {
            var client = new InfisicalClient(new ClientSettings()
            {
                Auth = new AuthenticationOptions()
                {
                    UniversalAuth = new UniversalAuthMethod()
                    {
                        ClientId = FeatureFlags.InfisicalClientId!,
                        ClientSecret = FeatureFlags.InfisicalClientSecret!
                    }
                },
                SiteUrl = string.IsNullOrEmpty(FeatureFlags.InfisicalEndpoint) ? "" : FeatureFlags.InfisicalEndpoint
            });
            ImportFromInfisical(this, client, FeatureFlags.InfisicalEnvironment, FeatureFlags.InfisicalProjectId);
        }
        ImportFromXml();
        ImportFromEnvironment(this);
    }

    private void SetValuesToDefault(object? instance)
    {
        if (instance == null)
            return;

        foreach (var prop in instance.GetType().GetProperties())
        {
            if (prop.PropertyType.IsClass && (prop.PropertyType.Assembly.FullName?.StartsWith("Adastral.Cockatoo.") ?? false))
            {
                bool n = false;
                var v = prop.GetValue(instance);
                if (v == null)
                {
                    v = Activator.CreateInstance(prop.PropertyType);
                    n = true;
                }
                SetValuesToDefault(v);
                if (n)
                {
                    prop.SetValue(instance, v);
                }
            }
            else
            {
                var attr = prop.GetCustomAttribute<DefaultValueAttribute>();
                if (attr == null)
                    return;
                prop.SetValue(instance, attr.Value);
            }
        }
    }

    private void ExportToInfisical(object? instance, string environment, string projectId, InfisicalClient client)
    {
        if (instance == null)
            return;
        foreach (var prop in instance.GetType().GetProperties())
        {
            if (prop.GetCustomAttribute<InfisicalIgnoreAttribute>() != null)
                continue;

            if (IsPropertyClass(prop))
            {
                var value = prop.GetValue(instance);
                if (value == null)
                {
                    value = Activator.CreateInstance(prop.PropertyType);
                    prop.SetValue(instance, value);
                }
                ExportToInfisical(value, environment, projectId, client);
            }
            else
            {
                var keyProp = prop.GetCustomAttribute<InfisicalKeyAttribute>();
                var pathProp = prop.GetCustomAttribute<InfisicalPathAttribute>();
                if (keyProp == null)
                    continue;

                var path = "/";
                if (pathProp != null)
                    path = pathProp.Path;
                if (path.StartsWith("/") == false)
                    path = $"/{path}";

                if (path.Length > 1)
                {
                    path = path.TrimEnd('/');
                }

                // update existing items
                bool found = false;
                var allSecrets = client.ListSecrets(new()
                {
                    Environment = environment,
                    ProjectId = projectId,
                    Path = path
                });
                foreach (var item in allSecrets)
                {
                    if (item.SecretKey.Trim().ToLower() == keyProp.Key.ToLower().Trim())
                    {
                        var value = prop.GetValue(instance);
                        string v = "";
                        if (value != null)
                        {
                            var x = value.ToString();
                            if (!string.IsNullOrEmpty(x))
                            {
                                v = x;
                            }
                        }
                        client.UpdateSecret(new()
                        {
                            ProjectId = projectId,
                            Environment = environment,

                            SecretName = item.SecretKey,
                            SecretValue = v,
                            Path = item.SecretPath
                        });
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    client.CreateSecret(new()
                    {
                        ProjectId = projectId,
                        Environment = environment,

                        SecretName = keyProp.Key,
                        Path = path
                    });
                }
            }
        }
    }

    private List<string> GenerateEnvironmentFileLines(object? instance)
    {
        if (instance == null)
            return [];

        var result = new List<string>();
        foreach (var prop in instance.GetType().GetProperties())
        {
            if (IsPropertyClass(prop))
            {
                var value = prop.GetValue(instance);
                if (value == null)
                {
                    value = Activator.CreateInstance(prop.PropertyType);
                    prop.SetValue(instance, value);
                }
                var x = GenerateEnvironmentFileLines(value);
                result.AddRange(x);
            }
            else
            {
                var keyProp = prop.GetCustomAttribute<EnvironmentKeyNameAttribute>();
                if (keyProp == null)
                    continue;

                var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null)
                {
                    var l = descAttr.Description
                        .Split("\n")
                        .SelectMany(v => v.Split("\\n"))
                        .Select(v => v.Split("\r")[0])
                        .Where(v => !string.IsNullOrEmpty(v))
                        .Select(v => $"# {v}").ToList();
                    result.AddRange(l);
                }

                var value = prop.GetValue(instance);
                if (value == null)
                {
                    result.Add($"{keyProp.Key}=");
                }
                else
                {
                    if (value is List<string> stringList)
                    {
                        result.Add($"{keyProp.Key}=" + string.Join(";", stringList.Select(v => v.Replace("\n", "\\n"))));
                    }
                    else if (value is string[] stringArray)
                    {
                        result.Add($"{keyProp.Key}=" + string.Join(";", stringArray.Select(v => v.Replace("\n", "\\n"))));
                    }
                    else if (value is IEnumerable<string> stringEnumerable)
                    {
                        result.Add($"{keyProp.Key}=" + string.Join(";", stringEnumerable.Select(v => v.Replace("\n", "\\n"))));
                    }
                    else
                    {
                        var s = value?.ToString() ?? "";
                        if (string.IsNullOrEmpty(s))
                            s = "";
                        result.Add($"{keyProp.Key}=" + s.Replace("\n", "\\n"));
                    }
                }
            }
        }
        return result;
    }

    private static bool IsPropertyClass(PropertyInfo prop)
    {
        return prop.PropertyType.IsClass && (prop.PropertyType.Assembly.FullName?.StartsWith("Adastral.Cockatoo.") ?? false);
    }
    private void ImportFromInfisical(object? instance, InfisicalClient client, string environment, string projectId)
    {
        if (instance == null)
            return;

        foreach (var prop in instance.GetType().GetProperties())
        {
            var ignoreProp = prop.GetCustomAttribute<InfisicalIgnoreAttribute>();
            if (ignoreProp != null)
                continue;
            if (IsPropertyClass(prop))
            {
                var pv = prop.GetValue(instance);
                bool pvi = false;
                if (pv == null)
                {
                    pv ??= Activator.CreateInstance(prop.PropertyType)!;
                    pvi = true;
                }
                _log.Info($"Calling for type {prop.Name} {prop.PropertyType.ToString()}");
                ImportFromInfisical(pv, client, environment, projectId);
                if (pvi)
                {
                    prop.SetValue(instance, pv);
                }
                continue;
            }
            var keyProp = prop.GetCustomAttribute<InfisicalKeyAttribute>();
            var pathProp = prop.GetCustomAttribute<InfisicalPathAttribute>();
            var categoryProp = prop.GetCustomAttribute<CategoryAttribute>();
            var defaultValueProp = prop.GetCustomAttribute<DefaultValueAttribute>();
            if (keyProp == null || string.IsNullOrEmpty(keyProp?.Key))
                continue;

            var path = "/";
            if (pathProp != null)
            {
                path = pathProp.Path;
            }
            else if (categoryProp != null)
            {
                path = categoryProp.Category;
            }

            if (string.IsNullOrEmpty(path))
                path = "/";
            else if (path.StartsWith("/") == false)
                path = $"/{path}";

            var opts = new GetSecretOptions()
            {
                ProjectId = projectId,
                Environment = environment,
                SecretName = keyProp!.Key,
                Path = path
            };
            bool exists = true;
            string? value = null;
            try
            {
                var allSecrets = client.ListSecrets(new()
                {
                    ProjectId = projectId,
                    Environment = environment,
                    Recursive = true
                });
                foreach (var x in allSecrets)
                {
                    if (x.SecretPath.Trim().ToLower() == opts.Path.Trim().ToLower())
                    {
                        opts.Path = x.SecretPath;
                    }

                    if (x.SecretKey.Trim().ToLower() == opts.SecretName.Trim().ToLower())
                    {
                        opts.SecretName = x.SecretKey;
                    }
                }

                var p = client.GetSecret(opts);
                value = p.SecretValue;
            }
            catch (InfisicalException iex)
            {
                if (iex.Message == $"Secret with name '{opts.SecretName}' not found.")
                {
                    exists = false;
                    value = null;
                }
                else
                {
                    throw new ApplicationException(
                        $"Failed to get secret for property {prop.Name} on {instance.GetType()}", iex);
                }
            }

            if (exists == false)
            {
                if (defaultValueProp != null)
                {
                    prop.SetValue(instance, defaultValueProp.Value);
                }

                continue;
            }

            var propType = prop.PropertyType;
            if (propType == typeof(string))
            {
                prop.SetValue(instance, value);
            }
            else if (propType == typeof(bool))
            {
                var x = value.Trim().ToLower();
                prop.SetValue(instance, x is "true" or "1");
            }
            else if (propType == typeof(bool?))
            {
                var x = value.Trim().ToLower();
                bool? v = null;
                if (!string.IsNullOrEmpty(x))
                {
                    v = x is "true" or "1";
                }
                prop.SetValue(instance, v);
            }
            else if (propType == typeof(int))
            {
                prop.SetValue(
                    instance,
                    int.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(int?))
            {
                prop.SetValue(
                    instance,
                    int.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(uint))
            {
                prop.SetValue(
                    instance,
                    uint.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(uint?))
            {
                prop.SetValue(
                    instance,
                    uint.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(long))
            {
                prop.SetValue(
                    instance,
                    long.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(long?))
            {
                prop.SetValue(
                    instance,
                    long.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(ulong))
            {
                prop.SetValue(
                    instance,
                    ulong.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(ulong?))
            {
                prop.SetValue(
                    instance,
                    ulong.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(byte))
            {
                prop.SetValue(
                    instance,
                    byte.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(byte?))
            {
                prop.SetValue(
                    instance,
                    byte.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(sbyte))
            {
                prop.SetValue(
                    instance,
                    sbyte.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(sbyte?))
            {
                prop.SetValue(
                    instance,
                    sbyte.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(short))
            {
                prop.SetValue(
                    instance,
                    short.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(short?))
            {
                prop.SetValue(
                    instance,
                    short.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(ushort))
            {
                prop.SetValue(
                    instance,
                    ushort.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(ushort?))
            {
                prop.SetValue(
                    instance,
                    ushort.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(float))
            {
                prop.SetValue(
                    instance,
                    float.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(float?))
            {
                prop.SetValue(
                    instance,
                    float.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(double))
            {
                prop.SetValue(
                    instance,
                    double.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(double?))
            {
                prop.SetValue(
                    instance,
                    double.TryParse(value, out var x) ? x : null);
            }
            else if (propType == typeof(decimal))
            {
                prop.SetValue(
                    instance,
                    double.TryParse(value, out var x) ? x : 0);
            }
            else if (propType == typeof(decimal?))
            {
                prop.SetValue(
                    instance,
                    decimal.TryParse(value, out var x) ? x : null);
            }
            else
            {
                throw new NotImplementedException($"Type {propType} has not been implemented (on property {prop.Name} in {instance.GetType()})");
            }
        }
    }

    private void ImportFromXml()
    {
        if (!FeatureFlags.ConfigXmlEnable)
        {
            return;
        }

        var location = FeatureFlags.ConfigXml;
        ImportFromXmlFile(location);
    }
    private void ImportFromXmlFile(string location)
    {
        if (!File.Exists(location))
        {
            _log.Error($"Cannot load from XML file since {location} does not exist");
            return;
        }

        var content = File.ReadAllText(location);
        var xmlSerializer = new XmlSerializer(typeof(CockatooConfig));
        var xmlTextReader = new XmlTextReader(new StringReader(content)) {XmlResolver = null};
        var data = (CockatooConfig?)xmlSerializer.Deserialize(xmlTextReader);
        if (data == null)
        {
            _log.Warn($"Deserialized XML to null from {location}");
            return;
        }

        foreach (var p in GetType().GetProperties())
        {
            p.SetValue(this, p.GetValue(data));
        }

        foreach (var f in GetType().GetFields())
        {
            f.SetValue(this, f.GetValue(data));
        }
    }

    private void ImportFromEnvironment(object? instance)
    {
        EnvironmentHelper.ParseEnvData(true);
        ImportFromEnvironment(EnvironmentHelper.Handler!, instance);
    }
    private void ImportFromEnvironment(EnvironmentFileHandler handler, object? instance)
    {
        if (instance == null)
            return;

        foreach (var prop in instance.GetType().GetProperties())
        {
            var propType = prop.PropertyType;
            if (propType.IsClass && (prop.PropertyType.Assembly.FullName?.StartsWith("Adastral.Cockatoo.") ?? false))
            {
                bool n = false;
                var v = prop.GetValue(instance);
                if (v == null)
                {
                    v = Activator.CreateInstance(propType);
                    n = true;
                }
                ImportFromEnvironment(handler, v);
                if (n)
                {
                    prop.SetValue(instance, v);
                }
            }
            else
            {
                var keyAttr = prop.GetCustomAttribute<EnvironmentKeyNameAttribute>();
                if (keyAttr == null)
                    continue;

                var e = handler.FindValue(keyAttr.Key);
                if (propType == typeof(string))
                {
                    if (e != null)
                    {
                        prop.SetValue(instance, e);
                    }
                }
                else if (propType == typeof(int))
                {
                    if (e != null)
                    {
                        if (int.TryParse(e, out var x))
                        {
                            prop.SetValue(instance, x);
                        }
                    }
                }
                else if (propType == typeof(int?))
                {
                    if (e != null)
                    {
                        prop.SetValue(instance, int.TryParse(e, out var x) ? x : null);
                    }
                }
                else if (propType == typeof(bool))
                {
                    if (e != null)
                    {
                        var x = e.Trim().ToLower();
                        prop.SetValue(instance, x is "true" or "1");
                    }
                }
                else if (propType == typeof(bool?))
                {
                    if (e != null)
                    {
                        var x = e.Trim().ToLower();
                        bool? m = null;
                        if (x is "true" or "1")
                            m = true;
                        else if (x is "false" or "0")
                            m = false;
                        if (m != null)
                        {
                            prop.SetValue(instance, m);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Base Url for public access. Must not have a leading slash (like <c>https://website.example.com/endpoint</c> or <c>https://example.com</c>).
    /// </summary>
    [XmlElement("PublicUrl")]
    [InfisicalKey("PublicUrl")]
    [EnvironmentKeyName("PUBLIC_URL")]
    [DefaultValue("http://localhost:5280")]
    [Description("Base Url for public access. Must not have a leading slash (like `https://website.example.com/endpoint` or `https://example.com`).")]
    public string PublicUrl { get; set; }

    /// <summary>
    /// Url that should be used for Partners when publishing applications.
    /// </summary>
    [XmlElement("PartnerUrl")]
    [InfisicalKey("PartnerUrl")]
    [EnvironmentKeyName("PARTNER_URL")]
    [DefaultValue(null)]
    [Description("Url that should be used for Partners when publishing applications.")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PartnerUrl { get; set; }

    [XmlElement("MongoDB")]
    public CockatooMongoConfig MongoDB { get; set; }

    [XmlElement("BasicAuth")]
    public CockatooBasicAuthConfig BasicAuth { get; set; }

    [XmlElement("Authentik")]
    public CockatooAuthentikConfig Authentik { get; set; }

    [XmlElement("LDAP")]
    public CockatooLdapConfig Ldap { get; set; }

    [XmlElement("Storage")]
    public CockatooStorageConfig Storage { get; set; }

    [XmlElement("AspNET")]
    public CockatooAspNetConfig AspNET { get; set; }

    [XmlElement("Southbank")]
    public CockatooSouthbankConfig Southbank { get; set; }

    [XmlElement("Redis")]
    public CockatooRedisConfig Redis { get; set; }
}
[Category("MongoDB")]
public class CockatooMongoConfig
{
    /// <summary>
    /// Connection String for MongoDB (Restart Required)
    /// </summary>
    [XmlElement("ConnectionString")]
    [InfisicalPath("/MongoDB")]
    [InfisicalKey("ConnectionString")]
    [EnvironmentKeyName("MONGO_CONNECTION")]
    [DefaultValue("")]
    [Description("Connection String for MongoDB (Restart Required)")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ConnectionString { get; set; }

    /// <summary>
    /// Database to use for MongoDB (Restart Required)
    /// </summary>
    [XmlElement("Database")]
    [InfisicalPath("/MongoDB")]
    [InfisicalKey("DatabaseName")]
    [EnvironmentKeyName("MONGO_DATABASE")]
    [DefaultValue("")]
    [Description("Database to use for MongoDB (Restart Required)")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string DatabaseName { get; set; }
}

[Category("Auth - Basic")]
public class CockatooBasicAuthConfig
{
    [InfisicalPath("BasicAuth")]
    [InfisicalKey("Enable")]
    [EnvironmentKeyName("AUTH_BASIC_ENABLE")]
    [DefaultValue(false)]
    [XmlElement(nameof(Enable))]
    public bool Enable { get; set; }
}

[Category("Authentik")]
public class CockatooAuthentikConfig
{
    [InfisicalPath("Authentik")]
    [InfisicalKey("Enable")]
    [EnvironmentKeyName("AUTHENTIK_ENABLE")]
    [DefaultValue(false)]
    public bool Enable { get; set; }

    [InfisicalPath("Authentik")]
    [InfisicalKey("ClientId")]
    [EnvironmentKeyName("AUTHENTIK_CLIENT_ID")]
    [DefaultValue("")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ClientId { get; set; }

    [InfisicalPath("Authentik")]
    [InfisicalKey("ClientSecret")]
    [EnvironmentKeyName("AUTHENTIK_CLIENT_SECRET")]
    [DefaultValue("")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ClientSecret { get; set; }

    [InfisicalPath("Authentik")]
    [InfisicalKey("Slug")]
    [EnvironmentKeyName("AUTHENTIK_SLUG")]
    [DefaultValue("")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Slug { get; set; }

    [InfisicalPath("Authentik")]
    [InfisicalKey("BaseUrl")]
    [EnvironmentKeyName("AUTHENTIK_DOMAIN")]
    [DefaultValue("")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string BaseUrl { get; set;  }
}

[Category("LDAP")]
public class CockatooLdapConfig
{
    [InfisicalPath("LDAP")]
    [InfisicalKey("Enable")]
    [EnvironmentKeyName("LDAP_ENABLE")]
    [DefaultValue(false)]
    [XmlElement(nameof(Enable))]
    public bool Enable { get; set; }

    [InfisicalPath("LDAP")]
    [InfisicalKey("Server")]
    [EnvironmentKeyName("LDAP_SERVER")]
    [DefaultValue(null)]
    [XmlElement(nameof(Server))]
    public string? Server { get; set; }
    [InfisicalPath("LDAP")]
    [InfisicalKey("Port")]
    [EnvironmentKeyName("LDAP_PORT")]
    [DefaultValue(389)]
    [XmlElement(nameof(Port))]
    public int Port { get; set; }

    [InfisicalPath("LDAP")]
    [InfisicalKey("Secure")]
    [EnvironmentKeyName("LDAP_SECURE")]
    [DefaultValue(false)]
    [XmlElement(nameof(Secure))]
    public bool Secure { get; set; } = false;

    [InfisicalPath("LDAP")]
    [InfisicalKey("BaseDN")]
    [EnvironmentKeyName("LDAP_BASEDN")]
    [DefaultValue("")]
    [XmlElement(nameof(BaseDN))]
    public string BaseDN { get; set; } = "";

    [InfisicalPath("LDAP")]
    [InfisicalKey("SearchQuery")]
    [EnvironmentKeyName("LDAP_SEARCH_QUERY")]
    [DefaultValue("")]
    [XmlElement(nameof(SearchQuery))]
    public string SearchQuery { get; set; } = "";

    [InfisicalPath("LDAP")]
    [InfisicalKey("Attributes")]
    [EnvironmentKeyName("LDAP_ATTRIBUTES")]
    [XmlElement(nameof(Attributes))]
    public string[] Attributes { get; set; } = [];

    [InfisicalPath("LDAP")]
    [InfisicalKey("SearchQuery")]
    [EnvironmentKeyName("LDAP_REQUIRED_GROUP")]
    [DefaultValue("")]
    [XmlElement(nameof(RequiredGroup))]
    public string RequiredGroup { get; set; } = "";

    [XmlElement("Formatting")]
    public CockatooLdapFormattingConfig Formatting { get; set; } = new();

    [XmlElement("ServiceAccount")]
    public CockatooLdapServiceAccountConfig ServiceAccount { get; set; } = new();
}

[Category("LDAP - Service Account")]
public class CockatooLdapServiceAccountConfig
{
    [InfisicalPath("/LDAP/ServiceAccount")]
    [InfisicalKey("Username")]
    [EnvironmentKeyName("LDAP_SERVICE_ACCOUNT_USERNAME")]
    [DefaultValue("")]
    public string Username { get; set; } = "";
    [InfisicalPath("/LDAP/ServiceAccount")]
    [InfisicalKey("Password")]
    [EnvironmentKeyName("LDAP_SERVICE_ACCOUNT_PASSWORD")]
    [DefaultValue("")]
    public string Password { get; set; } = "";
}

[Category("LDAP - Formatting of Distinguished Names")]
public class CockatooLdapFormattingConfig
{
    [InfisicalPath("/LDAP/Formatting")]
    [InfisicalKey("Username")]
    [EnvironmentKeyName("LDAP_FORMAT_USERNAME")]
    [DefaultValue("$1")]
    [XmlElement(nameof(Username))]
    public string Username { get; set; } = "$1";

    [InfisicalPath("/LDAP/Formatting")]
    [InfisicalKey("Group")]
    [EnvironmentKeyName("LDAP_FORMAT_GROUP")]
    [DefaultValue("$1")]
    [XmlElement(nameof(Group))]
    public string Group { get; set; } = "$1";
}

[Category("Storage")]
public class CockatooStorageConfig
{
    [XmlElement("Local")]
    public CockatooLocalStorageConfig Local { get; set; } = new();

    [XmlElement("FileApi")]
    public CockatooFileApiConfig FileApi { get; set; } = new();

    [XmlElement("S3")]
    public CockatooS3Config S3 { get; set; } = new();

    [InfisicalPath("/Storage")]
    [InfisicalKey("Proxy")]
    [EnvironmentKeyName("COCKATOO_STORAGE_PROXY")]
    [Description("When enabled, all files will be proxied through this Cockatoo instance.")]
    public bool Proxy { get; set; }
}

[Category("Storage - Local")]
public class CockatooLocalStorageConfig
{
    /// <summary>
    /// Use a Local Directory for file storage
    /// </summary>
    [InfisicalPath("/Storage/Local")]
    [InfisicalKey("Enable")]
    [EnvironmentKeyName("COCKATOO_STORAGE_LOCAL")]
    [DefaultValue(true)]
    [Description("Use a Local Directory for file storage")]
    public bool Enable { get; set; }

    /// <summary>
    /// Location to where storage should be when <see cref="Enable"/> is <see langword="true"/>
    /// </summary>
    [InfisicalPath("/Storage/Local")]
    [InfisicalKey("Location")]
    [EnvironmentKeyName("COCKATOO_STORAGE_LOCATION")]
    [DefaultValue("./storage")]
    [Description("Location to where storage should be when `Enable` is `true`")]
    public string Location { get; set; }
}

[Category("Storage - File Api")]
public class CockatooFileApiConfig
{
    /// <summary>
    /// HTTP Endpoint for all file serving. Must be the URL for a <c>Adastral.Cockatoo.FileWebAPI</c> instance
    /// </summary>
    [InfisicalPath("/Storage/FileApi")]
    [InfisicalKey("Url")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_ENDPOINT_FILE")]
    [Description("HTTP Endpoint for all file serving. Must be the URL for a deployed instance of `Adastral.Cockatoo.FileWebAPI`")]
    public string Endpoint { get; set; }

    /// <summary>
    /// When <see cref="Endpoint"/> is provided, and this is <see langword="true"/>, then just append <see cref="Adastral.Cockatoo.DataAccess.Models.StorageFileModel.Location"/> to <see cref="StorageFileEndpoint"/>
    /// </summary>
    [InfisicalPath("/Storage/FileApi")]
    [InfisicalKey("UseDirectLocation")]
    [DefaultValue(false)]
    [EnvironmentKeyName("COCKATOO_ENDPOINT_FILE_USEDIRECT")]
    [Description("When `Endpoint` is set, and this is `true`, then just append the `Location` property in `StorageFileModel` to the value of `StorageFileEndpoint`")]
    public bool UseDirect { get; set; }
}

[Category("Storage - S3")]
public class CockatooS3Config
{
    /// <summary>
    /// Enable the usage of S3 for file storage
    /// </summary>
    [XmlElement("Enable")]
    [InfisicalPath("/Storage/S3")]
    [InfisicalKey("Enable")]
    [DefaultValue(false)]
    [EnvironmentKeyName("COCKATOO_S3_ENABLE")]
    [Description("Enable the usage of S3 for file storage")]
    public bool Enable { get; set; }

    /// <summary>
    /// Bucket Name to use
    /// </summary>
    [XmlElement("Bucket")]
    [InfisicalPath("/Storage/S3")]
    [InfisicalKey("Bucket")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_S3_BUCKET")]
    [Description("Bucket Name to use")]
    public string BucketName { get; set; }

    /// <summary>
    /// Access Key Id for S3 Client
    /// </summary>
    [XmlElement("AccessKeyId")]
    [InfisicalPath("/Storage/S3")]
    [InfisicalKey("AccessKeyId")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_S3_ACCESS_KEY")]
    [Description("Access Key Id for S3 Client")]
    public string AccessKeyId { get; set; }

    /// <summary>
    /// Access Secret Key for S3 Client
    /// </summary>
    [XmlElement("AccessSecretKey")]
    [InfisicalPath("/Storage/S3")]
    [InfisicalKey("AccessKeySecret")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_S3_ACCESS_SECRET")]
    [Description("Access Secret Key for S3 Client")]
    public string AccessSecretKey { get; set; }

    /// <summary>
    /// AWS S3-Compatible Service URL
    /// </summary>
    [XmlElement("ServiceUrl")]
    [InfisicalPath("/Storage/S3")]
    [InfisicalKey("ServiceUrl")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_S3_SERVICE_URL")]
    [Description("AWS S3-Compatible Service URL")]
    public string ServiceUrl { get; set; }

    /// <summary>
    /// Is the S3 Service URL not AWS (e.g; Cloudflare R2)
    /// </summary>
    [XmlElement("NotUsingAWS")]
    [InfisicalPath("/Storage/S3")]
    [InfisicalKey("NotUsingAWS")]
    [DefaultValue(false)]
    [EnvironmentKeyName("COCKATOO_S3_NOAWS")]
    [Description("Is the S3 Service URL not AWS (e.g; Cloudflare R2)")]
    public bool NotUsingAWS { get; set; }

    /// <summary>
    /// Provide a specific region for authentication
    /// </summary>
    [XmlElement("AuthenticationRegion")]
    [InfisicalPath("/Storage/S3")]
    [InfisicalKey("AuthenticationRegion")]
    [DefaultValue(null)]
    [EnvironmentKeyName("COCKATOO_S3_AUTH_REGION")]
    [Description("Provide a specific region for authentication")]
    public string? AuthenticationRegion { get; set; }
}

[Category("Southbank")]
public class CockatooSouthbankConfig
{
    [XmlElement("v1")]
    [InfisicalPath("/Southbank/DownloadUrl")]
    [InfisicalKey("v1")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_SOUTHBANK_V1_DLURL")]
    [Description("Value for `dl_url` value in `/api/v1/Southbank`")]
    public string v1DownloadUrl { get; set; } = "";

    [XmlElement("v2")]
    [InfisicalPath("/Southbank/DownloadUrl")]
    [InfisicalKey("v2")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_SOUTHBANK_V2_DLURL")]
    [Description("Value for `dl_url` value in `/api/v2/Southbank`")]
    public string v2DownloadUrl { get; set; } = "";

    [XmlElement("v3")]
    [InfisicalPath("/Southbank/DownloadUrl")]
    [InfisicalKey("v3")]
    [DefaultValue("")]
    [EnvironmentKeyName("COCKATOO_SOUTHBANK_V3_DLURL")]
    [Description("Value for `dl_url` value in `/api/v3/Southbank`")]
    public string v3DownloadUrl { get; set; } = "";
}

[Category("Asp.NET")]
public class CockatooAspNetConfig
{
    /// <summary>
    /// Enable Swagger in Production (Restart Required)
    /// </summary>
    [XmlElement]
    [InfisicalPath("/AspNET")]
    [InfisicalKey("EnableSwagger")]
    [DefaultValue(false)]
    [EnvironmentKeyName("COCKATOO_SWAGGER_ENABLE")]
    [Description("Enable Swagger in Production (Restart Required)")]
    public bool SwaggerEnable { get; set; } = false;

    /// <summary>
    /// Request Header to use when doing token authentication.
    /// </summary>
    [XmlElement]
    [InfisicalPath("/AspNET")]
    [InfisicalKey("TokenHeader")]
    [DefaultValue("x-cockatoo-token")]
    [EnvironmentKeyName("COCKATOO_HEADER_TOKEN")]
    public string TokenHeader { get; set; } = "x-cockatoo-token";

    /// <summary>
    /// Is Cockatoo running behind a Proxy or a Load Balancer?
    /// </summary>
    [XmlElement]
    [InfisicalPath("/AspNET")]
    [InfisicalKey("BehindProxy")]
    [DefaultValue(false)]
    [EnvironmentKeyName("COCKATOO_BEHIND_PROXY")]
    [Description("Is Cockatoo running behind a Proxy or a Load Balancer?")]
    public bool BehindProxy { get; set; } = false;
}

[Category("Redis")]
public class CockatooRedisConfig
{
    /// <summary>
    /// When <see langword="false"/>, then an in-memory cache will be used. (Restart Required)
    /// </summary>
    [XmlElement]
    [InfisicalPath("Redis")]
    [InfisicalKey("Enable")]
    [DefaultValue(false)]
    [EnvironmentKeyName("REDIS_ENABLE")]
    [Description("When `false`, an in-memory cache will be used. (Restart Required)")]
    public bool Enable { get; set; } = false;

    /// <summary>
    /// Connection string for Redis. (Restart Required)
    /// </summary>
    [XmlElement]
    [InfisicalPath("Redis")]
    [InfisicalKey("ConnectionString")]
    [DefaultValue("")]
    [EnvironmentKeyName("REDIS_CONNECTION")]
    [Description("Connection string for Redis. (Restart Required)")]
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Instance Name. When empty, no instance name will be passed through.
    /// </summary>
    [XmlElement]
    [InfisicalPath("Redis")]
    [InfisicalKey("InstanceName")]
    [DefaultValue("cockatoo")]
    [EnvironmentKeyName("REDIS_INSTANCE_NAME")]
    [Description("Instance Name. When empty, no instance name will be passed through. (Restart Required)")]
    public string InstanceName { get; set; } = "cockatoo";
}