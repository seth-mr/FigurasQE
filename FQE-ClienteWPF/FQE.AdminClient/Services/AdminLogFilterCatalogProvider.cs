using System.IO;
using System.Text.Json;
using FQE.AdminClient.Models;

namespace FQE.AdminClient.Services;

public static class AdminLogFilterCatalogProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static AdminLogFilterCatalog Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "log-filter-options.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("No se encontro el archivo de catalogo de filtros.", path);
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AdminLogFilterCatalog>(json, JsonOptions) ?? new AdminLogFilterCatalog();
    }
}