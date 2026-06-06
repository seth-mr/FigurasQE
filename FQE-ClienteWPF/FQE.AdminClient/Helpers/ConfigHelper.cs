using System.IO;
using System.Text.Json;

namespace FQE.AdminClient.Helpers;

public class AppConfig
{
    public string GatewayUrl { get; set; } = "http://localhost:3000";
    public string LogsApiUrl { get; set; } = "http://localhost:3000/logs";
}

public static class ConfigHelper
{
    public static AppConfig LoadConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"No se encontró el archivo de configuración: {path}");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }
}
