namespace FigurasQE_WebClient.Services;

public static class ApiGatewayRoutes
{
    private const string DefaultBaseUrl = "http://localhost:3000";

    public static string InternalBaseUrl(IConfiguration configuration)
    {
        return NormalizeBaseUrl(
            configuration["ApiGateway:InternalBaseUrl"]
            ?? configuration["ApiGateway:BaseUrl"]
            ?? DefaultBaseUrl);
    }

    public static string PublicBaseUrl(IConfiguration configuration)
    {
        return NormalizeBaseUrl(
            configuration["ApiGateway:PublicBaseUrl"]
            ?? configuration["ApiGateway:BaseUrl"]
            ?? DefaultBaseUrl);
    }

    public static string InternalUrl(IConfiguration configuration, string path)
    {
        return BuildUrl(InternalBaseUrl(configuration), path);
    }

    private static string BuildUrl(string baseUrl, string path)
    {
        return $"{baseUrl}/{path.TrimStart('/')}";
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        return string.IsNullOrWhiteSpace(baseUrl)
            ? DefaultBaseUrl
            : baseUrl.Trim().TrimEnd('/');
    }
}
