using System.Diagnostics;
using System.Text.RegularExpressions;
using Adastral.Cockatoo.Common.Helpers;
using NLog;

namespace Adastral.Cockatoo.Common;

public static class FeatureFlags
{
    #region Parsing
    /// <inheritdoc cref="EnvironmentHelper.ParseBool"/>
    private static bool ParseBool(string environmentKey, bool defaultValue)
    {
        return EnvironmentHelper.ParseBool(environmentKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentHelper.ParseString"/>
    private static string ParseString(string environmentKey, string defaultValue)
    {
        return EnvironmentHelper.ParseString(environmentKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentHelper.ParseStringArray"/>
    private static string[] ParseStringArray(string envKey, string[] defaultValue)
    {
        return EnvironmentHelper.ParseStringArray(envKey, defaultValue);
    }

    /// <inheritdoc cref="EnvironmentHelper.ParseInt"/>
    private static int ParseInt(string envKey, int defaultValue)
    {
        return EnvironmentHelper.ParseInt(envKey, defaultValue);
    }
    #endregion

    #region Infisical
    public static string InfisicalClientId_Key => "INFISICAL_CLIENT_ID";
    public static string InfisicalClientSecret_Key => "INFISICAL_CLIENT_SECRET";
    public static string InfisicalProjectId_Key => "INFISICAL_PROJECT_ID";
    public static string InfisicalEndpoint_Key => "INFISICAL_ENDPOINT";
    public static string InfisicalEnvironment_Key => "INFISICAL_ENVIRONMENT";
    public static string InfisicalEnable_Key => "INFISICAL_ENABLED";
    public static string InfisicalClientId => ParseString(InfisicalClientId_Key, "");
    public static string InfisicalClientSecret => ParseString(InfisicalClientSecret_Key, "");
    public static string InfisicalProjectId => ParseString(InfisicalProjectId_Key, "");
    public static string InfisicalEndpoint => ParseString(InfisicalEndpoint_Key, "");
    public static string InfisicalEnvironment => ParseString(InfisicalEnvironment_Key, "");
    public static bool InfisicalEnable => ParseBool(InfisicalEnable_Key, false);
    #endregion

    public static bool ConfigXmlEnable => ParseBool("CONFIG_XML_ENABLE", false);
    public static string ConfigXml => ParseString("CONFIG_XML", "");
    
    /// <summary>
    /// <para>Enable Sentry integration</para>
    /// <para>Key: <c>COCKATOO_SENTRY</c></para>
    /// </summary>
    public static bool SentryEnable => ParseBool("COCKATOO_SENTRY", false);
    /// <summary>
    /// <para>Sentry DSN</para>
    /// <para>Key: <c>COCKATOO_SENTRY_DSN</c></para>
    /// </summary>
    public static string SentryDSN => ParseString("COCKATOO_SENTRY_DSN", "");
}