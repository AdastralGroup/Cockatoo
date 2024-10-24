using System.Security.Claims;

namespace Adastral.Cockatoo.Services.WebApi;

public interface IDirectAuthenticationProvider
{
    /// <summary>
    /// Is this authentication provider enabled?
    /// </summary>
    public bool Enabled();

    /// <summary>
    /// Get the name that should be displayed in the Audit Log or Session History
    /// </summary>
    public string GetName();

    /// <summary>
    /// Try and validate the <paramref name="username"/> and <paramref name="password"/> provided.
    /// </summary>
    /// <param name="username">Username to check</param>
    /// <param name="password">Password to use</param>
    /// <param name="principal">Result principal. Will be <see langword="null"/> when failed, and <see langword="false"/> will be returned.</param>
    /// <returns>
    /// <see langword="true"/> when successful, but <see langword="false"/> when failed to authenticate.
    /// </returns>
    public bool TryValidateCredentials(
        string username,
        string password,
        out ClaimsPrincipal? principal);
}