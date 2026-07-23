namespace IntegrationTests;

/// <summary>
/// Configuration for connecting to a NETCONF server.
/// Reads from environment variables set by docker-compose,
/// with fallbacks for local development.
/// </summary>
public static class NetconfConfig
{
    public static string Host =>
        Environment.GetEnvironmentVariable("NETCONF_HOST") ?? "localhost";

    public static int Port =>
        int.TryParse(Environment.GetEnvironmentVariable("NETCONF_PORT"), out var p) ? p : 830;

    public static string User =>
        Environment.GetEnvironmentVariable("NETCONF_USER") ?? "netconf";

    public static string Password =>
        Environment.GetEnvironmentVariable("NETCONF_PASSWORD") ?? "netconf";
}
