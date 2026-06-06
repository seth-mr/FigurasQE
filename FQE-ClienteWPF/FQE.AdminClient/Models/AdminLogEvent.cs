namespace FQE.AdminClient.Models;

public class AdminLogEvent
{
    public string Id { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string? Route { get; set; }
    public int? StatusCode { get; set; }
    public int? DurationMs { get; set; }
    private DateTime _timestamp;

    public DateTime Timestamp
    {
        get => _timestamp;
        set => _timestamp = value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : value;
    }
    public string Type { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
}