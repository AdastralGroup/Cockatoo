using System.DirectoryServices.Protocols;
using System.Security.Claims;
using System.Text;
using Adastral.Cockatoo.Common;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class LdapAuthenticationProvider : BaseService, IDirectAuthenticationProvider
{
    private readonly CockatooConfig _config;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public LdapAuthenticationProvider(IServiceProvider services)
        : base(services)
    {
        _config = services.GetRequiredService<CockatooConfig>();
    }

    /// <inheritdoc/>
    public string GetName()
    {
        return "pet.kate.cockatoo.LdapAuthenticationProvider";
    }

    /// <inheritdoc/>
    public bool Enabled()
    {
        return _config.Ldap.Enable
            && !string.IsNullOrEmpty(_config.Ldap.Server)
            && !string.IsNullOrEmpty(_config.Ldap.BaseDN);
    }

    /// <inheritdoc/>
    public bool TryValidateCredentials(string username, string password, out ClaimsPrincipal? principal)
    {
        principal = null;
        if (!Enabled())
            return false;

        if (!_config.Ldap.Enable || string.IsNullOrEmpty(_config.Ldap.Server))
            return false;
        try
        {
            var searchResults = SearchInAD(
                username,
                password,
                SearchScope.Subtree);

            var results = searchResults.Entries.Cast<SearchResultEntry>().Where(v => v.DistinguishedName.ToLower().StartsWith($"cn={username},"));
            if (results.Any())
            {
                var resultEntry = results.First();
                var groups = resultEntry.Attributes["memberOf"];
                var groupStringList = new List<string>();
                foreach (var x in groups)
                {
                    var groupNameBytes = x as byte[];
                    if (groupNameBytes == null)
                        continue;
                    var name = Encoding.Default.GetString(groupNameBytes).ToLower().Trim();
                    groupStringList.Add(name);
                }
                var requiredGroup = _config.Ldap.RequiredGroup.Replace("$basedn", _config.Ldap.BaseDN);
                if (!string.IsNullOrEmpty(requiredGroup))
                {
                    bool found = false;
                    foreach (var x in groups)
                    {
                        var groupNameBytes = x as byte[];
                        if (groupNameBytes == null)
                            continue;
                        var name = Encoding.Default.GetString(groupNameBytes).ToLower().Trim();
                        if (name == requiredGroup.Trim().ToLower())
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        _log.Debug($"User {username} is not in group {requiredGroup}");
                        return false;
                    }
                }

                var claims = new List<Claim>()
                {
                    new (ClaimTypes.NameIdentifier, resultEntry.Attributes["uid"][0].ToString()!),
                    new (ClaimTypes.Name, resultEntry.Attributes["name"][0].ToString()!),
                    new (ClaimTypes.Email, resultEntry.Attributes["mail"][0].ToString()!)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    "LDAP",
                    ClaimTypes.Name,
                    ClaimTypes.Role);
                principal = new ClaimsPrincipal(identity);
                return true;
            }
            else
            {
                _log.Warn($"Could not find user {username}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to validate Ldap Login for username {username}\n{ex}");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra("param.username", username);
            });
        }
        return false;
    }
    private SearchResponse SearchInAD(
        string username,
        string password,
        SearchScope scope,
        params string[] extraAttributes)
    {
        if (string.IsNullOrEmpty(_config.Ldap.Server))
        {
            throw new InvalidConfigurationValueException(
                $"Server address is required for LDAP functionality",
                nameof(_config.Ldap.Server),
                _config.Ldap);
        }

        var searchQuery = _config.Ldap.SearchQuery.Replace("$basedn", _config.Ldap.BaseDN);
        var basedn = _config.Ldap.BaseDN;
        string[] attrs = [
            ..extraAttributes,
            .._config.Ldap.Attributes
        ];

        var authType = AuthType.Basic;

        username = _config.Ldap.Formatting.Username
            .Replace("$1", username)
            .Replace("$basedn", _config.Ldap.BaseDN);

        var connection = new LdapConnection(
            new LdapDirectoryIdentifier(_config.Ldap.Server, _config.Ldap.Port))
        {
            AuthType = authType,
            Credential = new(username, password)
        };

        // the default one is v2 (at least in that version), and it is unknown if v3
        // is actually needed, but at least Synology LDAP works only with v3,
        // and since our Exchange doesn't complain, let it be v3
        connection.SessionOptions.ProtocolVersion = 3;

        if (_config.Ldap.Secure)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }

        connection.Bind();

        _log.Debug($"Searching scope: [{scope}], target: [{basedn}], query: [{searchQuery}]");
        var request = new SearchRequest(basedn, searchQuery, scope, attrs);

        return (SearchResponse)connection.SendRequest(request);
    }
}