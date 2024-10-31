using System.Security.Claims;
using System.Text;
using Adastral.Cockatoo.Common;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Novell.Directory.Ldap;
using Logger = NLog.Logger;

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
            var ldapUser = LocateLdapUser(username);
            if (ldapUser == null)
                return false;
            using (var currentUserConnection = ConnectToLdap(ldapUser.Dn, password))
            {
                if (!currentUserConnection.Bound)
                    return false;
            }
            var nameIdentAttr = GetAttribute(ldapUser, "uid");
            var nameAttr = GetAttribute(ldapUser, "name");
            var emailAttr = GetAttribute(ldapUser, "email");
            var claimList = new List<Claim>();
            if (nameIdentAttr != null)
            {
                claimList.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentAttr.StringValue));
            }
            if (nameAttr != null)
            {
                claimList.Add(new Claim(ClaimTypes.Name, nameAttr.StringValue));
            }
            if (emailAttr != null)
            {
                claimList.Add(new Claim(ClaimTypes.Email, emailAttr.StringValue));
            }
            var identity = new ClaimsIdentity(
                claimList,
                "LDAP",
                ClaimTypes.Name,
                ClaimTypes.Role);
            principal = new ClaimsPrincipal(identity);
            return true;
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
    public LdapAttribute? GetAttribute(LdapEntry userEntry, string attr)
    {
        var attributeSet = userEntry.GetAttributeSet();
        if (attributeSet.ContainsKey(attr))
        {
            return attributeSet.GetAttribute(attr);
        }

        _log.Warn("LDAP attribute {Attr} not found for user {User}", attr, userEntry.Dn);
        return null;
    }
    private static string SanitizeFilter(string input)
    {
        StringBuilder sanitizedinput = new StringBuilder();

        foreach (char c in input)
        {
            switch (c)
            {
                case '\\':
                    sanitizedinput.Append("\\5c");
                    break;
                case '*':
                    sanitizedinput.Append("\\2a");
                    break;
                case '(':
                    sanitizedinput.Append("\\28");
                    break;
                case ')':
                    sanitizedinput.Append("\\29");
                    break;
                case '\u0000': // Null character
                    sanitizedinput.Append("\\00");
                    break;
                default:
                    sanitizedinput.Append(c);
                    break;
            }
        }

        return sanitizedinput.ToString();
    }
    private LdapEntry? LocateLdapUser(string username)
    {
        using var ldapClient = ConnectToLdap();

        if (!ldapClient.Connected)
        {
            return null;
        }

        ldapClient.Constraints = GetSearchConstraints(
            ldapClient,
            _config.Ldap.ServiceAccount.Username.Replace("$basedn", _config.Ldap.BaseDN),
            _config.Ldap.ServiceAccount.Password);

        string sanitizedUsername = SanitizeFilter(username);

        string realSearchFilter = _config.Ldap.SearchFilter.Value
            .Replace("$basedn", _config.Ldap.BaseDN);
        if (realSearchFilter.Contains("$username"))
        {
            realSearchFilter = realSearchFilter.Replace("$username", username);
        }
        else
        {
            var b = new StringBuilder()
                .Append("(&")
                .Append(_config.Ldap.SearchFilter.Value.Replace("$basedn", _config.Ldap.BaseDN))
                .Append("(|");

            foreach (var attr in _config.Ldap.SearchFilter.Attributes)
            {
                b.Append($"({attr}={sanitizedUsername})");
            }
            b.Append("))");
            realSearchFilter = b.ToString();
        }

        _log.Debug(
            "LDAP Search: {BaseDn} {realSearchFilter} @ {LdapServer}",
            _config.Ldap.BaseDN,
            realSearchFilter,
            _config.Ldap.Server);

        ILdapSearchResults ldapUsers;

        string[] attrs = _config.Ldap.Attributes;

        try
        {
            ldapUsers = ldapClient.Search(
                _config.Ldap.BaseDN,
                LdapConnection.ScopeSub,
                realSearchFilter,
                attrs,
                false);
        }
        catch (LdapException e)
        {
            _log.Error(e, "Failed to filter users with: {Filter}", realSearchFilter);
            throw new ApplicationException("Error completing LDAP login while applying user filter.", e);
        }

        if (ldapUsers.HasMore())
        {
            LdapEntry ldapUser = ldapUsers.Next();

            if (ldapUsers.HasMore())
            {
                _log.Warn("More than one LDAP result matched; using first result only.");
            }

            _log.Debug("LDAP User: {ldapUser}", ldapUser);

            return ldapUser;
        }
        else
        {
            _log.Error("Found no users matching {Username} in LDAP search", username);
            throw new ApplicationException("Found no LDAP users matching provided username.");
        }
    }
    private LdapSearchConstraints GetSearchConstraints(
            LdapConnection ldapClient, string dn, string password)
    {
        var constraints = ldapClient.SearchConstraints;
        constraints.ReferralFollowing = true;
        constraints.setReferralHandler(new LdapAuthHandler(_log, dn, password));
        return constraints;
    }

    private LdapConnectionOptions GetConnectionOptions()
    {
        var connectionOptions = new LdapConnectionOptions();
        if (_config.Ldap.UseSsl)
        {
            connectionOptions.UseSsl();
        }

        return connectionOptions;
    }
    private LdapConnection ConnectToLdap(string? userDn = null, string? userPassword = null)
    {
        bool initialConnection = userDn == null;
        if (initialConnection)
        {
            userDn = _config.Ldap.ServiceAccount.Username.Replace("$basedn", _config.Ldap.BaseDN);
            userPassword = _config.Ldap.ServiceAccount.Password;
        }

        // not using `using` for the ability to return ldapClient, need to dispose this manually on exception
        var ldapClient = new LdapConnection(GetConnectionOptions());
        try
        {
            ldapClient.Connect(_config.Ldap.Server, _config.Ldap.Port);
            if (_config.Ldap.Secure)
            {
                ldapClient.StartTls();
            }

            _log.Debug("Trying bind as user {UserDn}", userDn);
            ldapClient.Bind(userDn, userPassword);
        }
        catch (Exception e)
        {
            ldapClient.Dispose();

            _log.Error(e, "Failed to Connect or Bind to server as user {UserDn}", userDn);
            var message = initialConnection
                ? "Failed to Connect or Bind to server."
                : "Error completing LDAP login. Invalid username or password.";
            throw new ApplicationException(message);
        }

        return ldapClient;
    }
}

internal sealed class LdapAuthHandler : ILdapAuthHandler
{
        private readonly ILogger _log;
        private readonly LdapAuthProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LdapAuthHandler" /> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="dn">The distinguished name to use when authenticating to the server.</param>
        /// <param name="password">The password to use when authenticating to the server.</param>
        public LdapAuthHandler(ILogger logger, string dn, string password)
        {
            _log = logger;
            _provider = new LdapAuthProvider(dn, Encoding.UTF8.GetBytes(password));
        }

        /// <inheritdoc />
        public LdapAuthProvider GetAuthProvider(string host, int port)
        {
            _log.Debug("Referred to {Host}:{Port}. Trying bind as user {Dn}", host, port, _provider.Dn);
            return _provider;
        }
}